
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LabEquipmentCheckout
{
    public partial class FormMain : Form
    {
        private readonly BindingList<BorrowRecord> records = new();
        private readonly BindingSource source = new();
        private readonly ErrorProvider errors = new();

        private readonly TextBox txtBorrowId = new();
        private readonly TextBox txtStudentId = new();
        private readonly TextBox txtStudentName = new();
        private readonly ComboBox cboEquipment = new();
        private readonly NumericUpDown numQuantity = new();
        private readonly DateTimePicker dtpBorrowDate = new();
        private readonly DateTimePicker dtpDueDate = new();
        private readonly DateTimePicker dtpReturnDate = new();
        private readonly ComboBox cboStatus = new();

        private readonly TextBox txtSearch = new();
        private readonly ComboBox cboFilter = new();
        private readonly DataGridView dgv = new();

        private readonly ToolStripStatusLabel lblTotal = new();
        private readonly ToolStripStatusLabel lblOverdue = new();

        private BorrowRecord? selected;

        private readonly string[] statuses =
        {
            "Đang mượn",
            "Đã trả",
            "Quá hạn"
        };

        public FormMain()
        {
            Text = "Lab Equipment Checkout";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1250, 780);
            MinimumSize = new Size(1000, 650);
            Font = new Font("Segoe UI", 9);

            errors.ContainerControl = this;
            errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            LoadSamples();
            UpdateOverdueStatuses();
            RefreshData();
            ClearFields();
        }

        private void BuildInterface()
        {
            var menu = new MenuStrip();
            var menuFile = new ToolStripMenuItem("Tệp");
            var menuExport = new ToolStripMenuItem("Xuất CSV");
            var menuExit = new ToolStripMenuItem("Thoát");

            menuExport.Click += (_, _) => ExportCsv();
            menuExit.Click += (_, _) => Close();

            menuFile.DropDownItems.Add(menuExport);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add(menuExit);
            menu.Items.Add(menuFile);
            MainMenuStrip = menu;

            var statusBar = new StatusStrip();
            statusBar.Items.Add(lblTotal);
            statusBar.Items.Add(new ToolStripStatusLabel { Spring = true });
            statusBar.Items.Add(lblOverdue);

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8)
            };

            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 310));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var inputGroup = new GroupBox
            {
                Text = "Thông tin phiếu mượn",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            var input = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5
            };

            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < 5; i++)
                input.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            ConfigureText(txtBorrowId);
            ConfigureText(txtStudentId);
            ConfigureText(txtStudentName);

            ConfigureCombo(cboEquipment);
            cboEquipment.Items.AddRange(new object[]
            {
                "Máy tính",
                "Laptop",
                "Máy chiếu",
                "Bàn phím",
                "Chuột",
                "Arduino",
                "Kit thực hành mạng",
                "Bộ thí nghiệm điện"
            });

            numQuantity.Minimum = 1;
            numQuantity.Maximum = 10000;
            numQuantity.Dock = DockStyle.Fill;

            ConfigureDate(dtpBorrowDate);
            ConfigureDate(dtpDueDate);
            ConfigureDate(dtpReturnDate);

            dtpBorrowDate.Value = DateTime.Today;
            dtpDueDate.Value = DateTime.Today.AddDays(7);
            dtpReturnDate.ShowCheckBox = true;
            dtpReturnDate.Checked = false;
            dtpReturnDate.Value = DateTime.Today;

            ConfigureCombo(cboStatus);
            cboStatus.Items.AddRange(statuses);

            AddField(input, "Mã phiếu:", txtBorrowId, 0, 0);
            AddField(input, "Mã sinh viên:", txtStudentId, 1, 0);
            AddField(input, "Họ tên sinh viên:", txtStudentName, 0, 1);
            AddField(input, "Thiết bị:", cboEquipment, 1, 1);
            AddField(input, "Số lượng:", numQuantity, 0, 2);
            AddField(input, "Ngày mượn:", dtpBorrowDate, 1, 2);
            AddField(input, "Hạn trả:", dtpDueDate, 0, 3);
            AddField(input, "Ngày trả thực tế:", dtpReturnDate, 1, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true
            };

            var btnAdd = new Button { Text = "Thêm", Width = 85 };
            var btnUpdate = new Button { Text = "Cập nhật", Width = 95 };
            var btnDelete = new Button { Text = "Xóa", Width = 85 };
            var btnReturn = new Button { Text = "Đánh dấu đã trả", Width = 135 };
            var btnClear = new Button { Text = "Làm mới", Width = 85 };

            btnAdd.Click += (_, _) => AddRecord();
            btnUpdate.Click += (_, _) => UpdateRecord();
            btnDelete.Click += (_, _) => DeleteRecord();
            btnReturn.Click += (_, _) => MarkAsReturned();
            btnClear.Click += (_, _) => ClearFields();

            buttons.Controls.AddRange(new Control[]
            {
                btnAdd, btnUpdate, btnDelete, btnReturn, btnClear
            });

            input.Controls.Add(new Label
            {
                Text = "Trạng thái:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 4);

            input.Controls.Add(cboStatus, 1, 4);
            input.Controls.Add(buttons, 2, 4);
            input.SetColumnSpan(buttons, 2);

            inputGroup.Controls.Add(input);

            var listGroup = new GroupBox
            {
                Text = "Danh sách phiếu mượn",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            var listLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true,
                AutoScroll = true
            };

            txtSearch.Width = 250;
            txtSearch.PlaceholderText = "Tìm mã sinh viên hoặc tên thiết bị";
            txtSearch.TextChanged += (_, _) => RefreshData();

            ConfigureCombo(cboFilter);
            cboFilter.Width = 150;
            cboFilter.Items.AddRange(new object[]
            {
                "Tất cả",
                "Đang mượn",
                "Quá hạn",
                "Đã trả"
            });
            cboFilter.SelectedIndex = 0;
            cboFilter.SelectedIndexChanged += (_, _) => RefreshData();

            filterPanel.Controls.Add(new Label
            {
                Text = "Tìm kiếm:",
                AutoSize = true,
                Margin = new Padding(3, 8, 3, 3)
            });

            filterPanel.Controls.Add(txtSearch);

            filterPanel.Controls.Add(new Label
            {
                Text = "Trạng thái:",
                AutoSize = true,
                Margin = new Padding(8, 8, 3, 3)
            });

            filterPanel.Controls.Add(cboFilter);

            ConfigureGrid();

            listLayout.Controls.Add(filterPanel, 0, 0);
            listLayout.Controls.Add(dgv, 0, 1);
            listGroup.Controls.Add(listLayout);

            main.Controls.Add(inputGroup, 0, 0);
            main.Controls.Add(listGroup, 0, 1);

            Controls.Add(main);
            Controls.Add(statusBar);
            Controls.Add(menu);
        }

        private void ConfigureText(TextBox text)
        {
            text.Dock = DockStyle.Fill;
        }

        private void ConfigureCombo(ComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Dock = DockStyle.Fill;
        }

        private void ConfigureDate(DateTimePicker date)
        {
            date.Format = DateTimePickerFormat.Custom;
            date.CustomFormat = "dd/MM/yyyy";
            date.Dock = DockStyle.Fill;
        }

        private void AddField(
            TableLayoutPanel table,
            string title,
            Control control,
            int pair,
            int row)
        {
            int col = pair * 2;

            table.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, col, row);

            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(3, 5, 8, 5);
            table.Controls.Add(control, col + 1, row);
        }

        private void ConfigureGrid()
        {
            dgv.Dock = DockStyle.Fill;
            dgv.AutoGenerateColumns = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.RowHeadersVisible = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.Columns.Clear();

            AddColumn("BorrowId", "Mã phiếu");
            AddColumn("StudentId", "Mã sinh viên");
            AddColumn("StudentName", "Họ tên");
            AddColumn("Equipment", "Thiết bị");
            AddColumn("Quantity", "Số lượng");
            AddColumn("BorrowDate", "Ngày mượn", "dd/MM/yyyy");
            AddColumn("DueDate", "Hạn trả", "dd/MM/yyyy");
            AddColumn("ActualReturnDate", "Ngày trả thực tế", "dd/MM/yyyy");
            AddColumn("Status", "Trạng thái");

            dgv.SelectionChanged += (_, _) => LoadSelected();
        }

        private void AddColumn(
            string property,
            string title,
            string? format = null)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                Name = property,
                HeaderText = title,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

            if (format != null)
                column.DefaultCellStyle.Format = format;

            dgv.Columns.Add(column);
        }

        private void LoadSamples()
        {
            records.Add(new BorrowRecord
            {
                BorrowId = "PM001",
                StudentId = "SV001",
                StudentName = "Nguyễn Văn An",
                Equipment = "Laptop",
                Quantity = 1,
                BorrowDate = DateTime.Today.AddDays(-5),
                DueDate = DateTime.Today.AddDays(-2),
                Status = "Đang mượn"
            });

            records.Add(new BorrowRecord
            {
                BorrowId = "PM002",
                StudentId = "SV002",
                StudentName = "Trần Thị Bình",
                Equipment = "Chuột",
                Quantity = 2,
                BorrowDate = DateTime.Today.AddDays(-2),
                DueDate = DateTime.Today.AddDays(5),
                Status = "Đang mượn"
            });

            records.Add(new BorrowRecord
            {
                BorrowId = "PM003",
                StudentId = "SV003",
                StudentName = "Lê Minh Cường",
                Equipment = "Máy chiếu",
                Quantity = 1,
                BorrowDate = DateTime.Today.AddDays(-10),
                DueDate = DateTime.Today.AddDays(-3),
                ActualReturnDate = DateTime.Today.AddDays(-4),
                Status = "Đã trả"
            });
        }

        private void UpdateOverdueStatuses()
        {
            foreach (var record in records)
            {
                if (record.ActualReturnDate.HasValue)
                    record.Status = "Đã trả";
                else if (record.DueDate.Date < DateTime.Today)
                    record.Status = "Quá hạn";
                else
                    record.Status = "Đang mượn";
            }
        }

        private void RefreshData()
        {
            UpdateOverdueStatuses();

            string keyword = txtSearch.Text.Trim();

            var filtered = records
                .Where(r =>
                    r.StudentId.Contains(
                        keyword, StringComparison.OrdinalIgnoreCase)
                    || r.Equipment.Contains(
                        keyword, StringComparison.OrdinalIgnoreCase))
                .Where(r =>
                    cboFilter.SelectedIndex <= 0
                    || r.Status == cboFilter.SelectedItem?.ToString())
                .ToList();

            source.DataSource = new BindingList<BorrowRecord>(filtered);
            dgv.DataSource = source;

            lblTotal.Text = $"Tổng số phiếu: {records.Count}";
            lblOverdue.Text =
                $"Số phiếu quá hạn: {records.Count(r => r.Status == "Quá hạn")}";
        }

        private void LoadSelected()
        {
            if (dgv.CurrentRow?.DataBoundItem is not BorrowRecord record)
                return;

            selected = record;

            txtBorrowId.Text = record.BorrowId;
            txtStudentId.Text = record.StudentId;
            txtStudentName.Text = record.StudentName;
            cboEquipment.SelectedItem = record.Equipment;
            numQuantity.Value = Math.Max(
                numQuantity.Minimum,
                Math.Min(numQuantity.Maximum, record.Quantity));
            dtpBorrowDate.Value = record.BorrowDate;
            dtpDueDate.Value = record.DueDate;

            dtpReturnDate.Checked = record.ActualReturnDate.HasValue;
            dtpReturnDate.Value =
                record.ActualReturnDate ?? DateTime.Today;

            cboStatus.SelectedItem = record.Status;
        }

        private bool ValidateFields()
        {
            errors.Clear();
            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtBorrowId.Text))
            {
                errors.SetError(txtBorrowId, "Vui lòng nhập mã phiếu.");
                valid = false;
            }
            else if (records.Any(r =>
                r.BorrowId.Equals(
                    txtBorrowId.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase)
                && r != selected))
            {
                errors.SetError(txtBorrowId, "Mã phiếu đã tồn tại.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtStudentId.Text))
            {
                errors.SetError(txtStudentId, "Vui lòng nhập mã sinh viên.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtStudentName.Text))
            {
                errors.SetError(txtStudentName, "Vui lòng nhập họ tên sinh viên.");
                valid = false;
            }

            if (cboEquipment.SelectedIndex < 0)
            {
                errors.SetError(cboEquipment, "Vui lòng chọn thiết bị.");
                valid = false;
            }

            if (numQuantity.Value <= 0)
            {
                errors.SetError(numQuantity, "Số lượng phải lớn hơn 0.");
                valid = false;
            }

            if (dtpDueDate.Value.Date < dtpBorrowDate.Value.Date)
            {
                errors.SetError(dtpDueDate, "Hạn trả không được trước ngày mượn.");
                valid = false;
            }

            if (dtpReturnDate.Checked &&
                dtpReturnDate.Value.Date < dtpBorrowDate.Value.Date)
            {
                errors.SetError(
                    dtpReturnDate,
                    "Ngày trả thực tế không được trước ngày mượn.");
                valid = false;
            }

            if (cboStatus.SelectedIndex < 0)
            {
                errors.SetError(cboStatus, "Vui lòng chọn trạng thái.");
                valid = false;
            }

            if (cboStatus.SelectedItem?.ToString() == "Đã trả"
                && !dtpReturnDate.Checked)
            {
                errors.SetError(
                    dtpReturnDate,
                    "Hãy chọn ngày trả thực tế khi đánh dấu đã trả.");
                valid = false;
            }

            return valid;
        }

        private BorrowRecord ReadFields()
        {
            string status = cboStatus.SelectedItem?.ToString() ?? "Đang mượn";

            DateTime? returnDate = null;

            if (status == "Đã trả")
                returnDate = dtpReturnDate.Checked
                    ? dtpReturnDate.Value.Date
                    : DateTime.Today;
            else if (dtpReturnDate.Checked)
                returnDate = dtpReturnDate.Value.Date;

            var record = new BorrowRecord
            {
                BorrowId = txtBorrowId.Text.Trim(),
                StudentId = txtStudentId.Text.Trim(),
                StudentName = txtStudentName.Text.Trim(),
                Equipment = cboEquipment.SelectedItem?.ToString() ?? "",
                Quantity = (int)numQuantity.Value,
                BorrowDate = dtpBorrowDate.Value.Date,
                DueDate = dtpDueDate.Value.Date,
                ActualReturnDate = returnDate,
                Status = status
            };

            if (record.ActualReturnDate.HasValue)
                record.Status = "Đã trả";
            else if (record.DueDate.Date < DateTime.Today)
                record.Status = "Quá hạn";
            else
                record.Status = "Đang mượn";

            return record;
        }

        private void AddRecord()
        {
            if (!ValidateFields())
                return;

            records.Add(ReadFields());
            RefreshData();
            ClearFields();

            MessageBox.Show("Thêm phiếu mượn thành công.");
        }

        private void UpdateRecord()
        {
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu cần cập nhật.");
                return;
            }

            if (!ValidateFields())
                return;

            BorrowRecord updated = ReadFields();

            selected.BorrowId = updated.BorrowId;
            selected.StudentId = updated.StudentId;
            selected.StudentName = updated.StudentName;
            selected.Equipment = updated.Equipment;
            selected.Quantity = updated.Quantity;
            selected.BorrowDate = updated.BorrowDate;
            selected.DueDate = updated.DueDate;
            selected.ActualReturnDate = updated.ActualReturnDate;
            selected.Status = updated.Status;

            RefreshData();
            ClearFields();

            MessageBox.Show("Cập nhật phiếu mượn thành công.");
        }

        private void DeleteRecord()
        {
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu cần xóa.");
                return;
            }

            var answer = MessageBox.Show(
                $"Bạn có chắc muốn xóa phiếu {selected.BorrowId}?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
                return;

            records.Remove(selected);
            RefreshData();
            ClearFields();
        }

        private void MarkAsReturned()
        {
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu cần trả.");
                return;
            }

            if (selected.ActualReturnDate.HasValue)
            {
                MessageBox.Show("Phiếu này đã được trả trước đó.");
                return;
            }

            DateTime returnDate = DateTime.Today;

            if (returnDate < selected.BorrowDate.Date)
            {
                MessageBox.Show(
                    "Ngày trả thực tế không được trước ngày mượn.",
                    "Dữ liệu không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var answer = MessageBox.Show(
                $"Xác nhận trả thiết bị của phiếu {selected.BorrowId} hôm nay?",
                "Xác nhận trả thiết bị",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
                return;

            selected.ActualReturnDate = returnDate;
            selected.Status = "Đã trả";

            RefreshData();
            ClearFields();

            MessageBox.Show("Đã cập nhật ngày trả và trạng thái.");
        }

        private void ClearFields()
        {
            selected = null;

            txtBorrowId.Clear();
            txtStudentId.Clear();
            txtStudentName.Clear();

            cboEquipment.SelectedIndex = -1;
            numQuantity.Value = 1;

            dtpBorrowDate.Value = DateTime.Today;
            dtpDueDate.Value = DateTime.Today.AddDays(7);
            dtpReturnDate.Value = DateTime.Today;
            dtpReturnDate.Checked = false;

            cboStatus.SelectedItem = "Đang mượn";

            errors.Clear();

            dgv.ClearSelection();
            dgv.CurrentCell = null;
        }

        private void ExportCsv()
        {
            var visible = source.Cast<BorrowRecord>().ToList();

            using var dialog = new SaveFileDialog
            {
                Title = "Xuất danh sách phiếu mượn",
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "DanhSachPhieuMuon.csv",
                DefaultExt = "csv",
                AddExtension = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            using var writer = new StreamWriter(
                dialog.FileName, false, new UTF8Encoding(true));

            writer.WriteLine(
                "MaPhieu,MaSinhVien,HoTen,ThietBi,SoLuong,NgayMuon,HanTra,NgayTraThucTe,TrangThai");

            foreach (var r in visible)
            {
                writer.WriteLine(string.Join(",",
                    Escape(r.BorrowId),
                    Escape(r.StudentId),
                    Escape(r.StudentName),
                    Escape(r.Equipment),
                    r.Quantity,
                    Escape(r.BorrowDate.ToString("dd/MM/yyyy")),
                    Escape(r.DueDate.ToString("dd/MM/yyyy")),
                    Escape(r.ActualReturnDate?.ToString("dd/MM/yyyy") ?? ""),
                    Escape(r.Status)));
            }

            MessageBox.Show($"Đã xuất {visible.Count} phiếu mượn.");
        }

        private string Escape(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
