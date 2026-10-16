
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ClinicAppointmentManager
{
    public partial class FormMain : Form
    {
        private readonly BindingList<Appointment> appointments = new();
        private readonly BindingSource source = new();
        private readonly ErrorProvider errors = new();

        private readonly TextBox txtId = new();
        private readonly TextBox txtName = new();
        private readonly TextBox txtPhone = new();
        private readonly TextBox txtReason = new();
        private readonly TextBox txtSearch = new();

        private readonly DateTimePicker dtpDate = new();
        private readonly DateTimePicker dtpFilterDate = new();

        private readonly ComboBox cboDoctor = new();
        private readonly ComboBox cboStatus = new();
        private readonly ComboBox cboFilterStatus = new();

        private readonly CheckBox chkDate = new();
        private readonly DataGridView dgv = new();
        private readonly ToolStripStatusLabel lblCount = new();

        private Appointment? selected;

        private readonly string[] statuses =
        {
            "Chờ khám", "Đang khám", "Hoàn tất", "Đã hủy"
        };

        public FormMain()
        {
            Text = "Clinic Appointment Manager";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1250, 780);
            MinimumSize = new Size(1000, 650);
            Font = new Font("Segoe UI", 9);
            errors.ContainerControl = this;
            errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            LoadSamples();
            RefreshData();
            ClearFields();
        }

        private void BuildInterface()
        {
            var menu = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Tệp");
            var exportMenu = new ToolStripMenuItem("Xuất CSV");
            var exitMenu = new ToolStripMenuItem("Thoát");

            exportMenu.Click += (_, _) => ExportCsv();
            exitMenu.Click += (_, _) => Close();

            fileMenu.DropDownItems.Add(exportMenu);
            fileMenu.DropDownItems.Add(exitMenu);
            menu.Items.Add(fileMenu);
            MainMenuStrip = menu;

            var statusBar = new StatusStrip();
            statusBar.Items.Add(lblCount);

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8)
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 290));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var groupInput = new GroupBox
            {
                Text = "Thông tin lịch hẹn",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            var input = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5
            };

            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < 5; i++)
                input.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            ConfigureText(txtId);
            ConfigureText(txtName);
            ConfigureText(txtPhone);
            ConfigureText(txtReason);
            ConfigureText(txtSearch);

            dtpDate.Format = DateTimePickerFormat.Custom;
            dtpDate.CustomFormat = "dd/MM/yyyy HH:mm";
            dtpDate.ShowUpDown = true;
            dtpDate.Dock = DockStyle.Fill;

            ConfigureCombo(cboDoctor);
            cboDoctor.Items.AddRange(new object[]
            {
                "Bác sĩ Nguyễn Văn An",
                "Bác sĩ Trần Thị Bình",
                "Bác sĩ Lê Minh Cường",
                "Bác sĩ Phạm Thu Hà"
            });

            ConfigureCombo(cboStatus);
            cboStatus.Items.AddRange(statuses);

            AddField(input, "Mã lịch hẹn:", txtId, 0, 0);
            AddField(input, "Họ tên bệnh nhân:", txtName, 1, 0);
            AddField(input, "Số điện thoại:", txtPhone, 0, 1);
            AddField(input, "Ngày giờ hẹn:", dtpDate, 1, 1);
            AddField(input, "Bác sĩ:", cboDoctor, 0, 2);
            AddField(input, "Trạng thái:", cboStatus, 1, 2);
            AddField(input, "Lý do khám:", txtReason, 0, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true
            };

            var btnAdd = new Button { Text = "Thêm", Width = 90 };
            var btnUpdate = new Button { Text = "Cập nhật", Width = 100 };
            var btnDelete = new Button { Text = "Xóa", Width = 90 };
            var btnClear = new Button { Text = "Làm mới", Width = 90 };

            btnAdd.Click += (_, _) => AddAppointment();
            btnUpdate.Click += (_, _) => UpdateAppointment();
            btnDelete.Click += (_, _) => DeleteAppointment();
            btnClear.Click += (_, _) => ClearFields();

            buttons.Controls.AddRange(new Control[]
            {
                btnAdd, btnUpdate, btnDelete, btnClear
            });

            input.Controls.Add(buttons, 1, 4);
            input.SetColumnSpan(buttons, 3);
            groupInput.Controls.Add(input);

            var groupList = new GroupBox
            {
                Text = "Danh sách lịch hẹn",
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

            var filters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true,
                AutoScroll = true
            };

            txtSearch.Width = 220;
            txtSearch.PlaceholderText = "Tìm tên hoặc số điện thoại";
            txtSearch.TextChanged += (_, _) => RefreshData();

            chkDate.Text = "Lọc ngày";
            chkDate.AutoSize = true;
            chkDate.CheckedChanged += (_, _) =>
            {
                dtpFilterDate.Enabled = chkDate.Checked;
                RefreshData();
            };

            dtpFilterDate.Format = DateTimePickerFormat.Short;
            dtpFilterDate.Width = 120;
            dtpFilterDate.Enabled = false;
            dtpFilterDate.ValueChanged += (_, _) => RefreshData();

            ConfigureCombo(cboFilterStatus);
            cboFilterStatus.Width = 150;
            cboFilterStatus.Items.Add("Tất cả trạng thái");
            cboFilterStatus.Items.AddRange(statuses);
            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged += (_, _) => RefreshData();

            filters.Controls.Add(new Label
            {
                Text = "Tìm kiếm:",
                AutoSize = true,
                Margin = new Padding(3, 8, 3, 3)
            });
            filters.Controls.Add(txtSearch);
            filters.Controls.Add(chkDate);
            filters.Controls.Add(dtpFilterDate);
            filters.Controls.Add(cboFilterStatus);

            ConfigureGrid();

            listLayout.Controls.Add(filters, 0, 0);
            listLayout.Controls.Add(dgv, 0, 1);
            groupList.Controls.Add(listLayout);

            main.Controls.Add(groupInput, 0, 0);
            main.Controls.Add(groupList, 0, 1);

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

            AddColumn("AppointmentId", "Mã lịch hẹn");
            AddColumn("PatientName", "Bệnh nhân");
            AddColumn("Phone", "Số điện thoại");
            AddColumn("AppointmentDate", "Ngày giờ hẹn", "dd/MM/yyyy HH:mm");
            AddColumn("Doctor", "Bác sĩ");
            AddColumn("Reason", "Lý do khám");
            AddColumn("Status", "Trạng thái");

            dgv.SelectionChanged += (_, _) =>
            {
                if (dgv.CurrentRow?.DataBoundItem is Appointment a)
                {
                    selected = a;
                    txtId.Text = a.AppointmentId;
                    txtName.Text = a.PatientName;
                    txtPhone.Text = a.Phone;
                    dtpDate.Value = a.AppointmentDate;
                    cboDoctor.SelectedItem = a.Doctor;
                    txtReason.Text = a.Reason;
                    cboStatus.SelectedItem = a.Status;
                }
            };
        }

        private void AddColumn(string property, string header, string? format = null)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

            if (format != null)
                column.DefaultCellStyle.Format = format;

            dgv.Columns.Add(column);
        }

        private void LoadSamples()
        {
            appointments.Add(new Appointment
            {
                AppointmentId = "LH001",
                PatientName = "Nguyễn Văn Minh",
                Phone = "0912345678",
                AppointmentDate = DateTime.Today.AddDays(1).AddHours(8),
                Doctor = "Bác sĩ Nguyễn Văn An",
                Reason = "Khám tổng quát",
                Status = "Chờ khám"
            });

            appointments.Add(new Appointment
            {
                AppointmentId = "LH002",
                PatientName = "Trần Thị Lan",
                Phone = "0987654321",
                AppointmentDate = DateTime.Today.AddDays(1).AddHours(9),
                Doctor = "Bác sĩ Trần Thị Bình",
                Reason = "Đau đầu",
                Status = "Đang khám"
            });
        }

        private void RefreshData()
        {
            string keyword = txtSearch.Text.Trim();

            var list = appointments.Where(a =>
                    a.PatientName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || a.Phone.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Where(a => !chkDate.Checked ||
                    a.AppointmentDate.Date == dtpFilterDate.Value.Date)
                .Where(a => cboFilterStatus.SelectedIndex <= 0 ||
                    a.Status == cboFilterStatus.SelectedItem?.ToString())
                .ToList();

            source.DataSource = new BindingList<Appointment>(list);
            dgv.DataSource = source;
            lblCount.Text = $"Số lịch hẹn đang hiển thị: {list.Count}";
        }

        private bool ValidateFields()
        {
            errors.Clear();
            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                errors.SetError(txtId, "Vui lòng nhập mã lịch hẹn.");
                valid = false;
            }
            else if (appointments.Any(a =>
                a.AppointmentId.Equals(txtId.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase) && a != selected))
            {
                errors.SetError(txtId, "Mã lịch hẹn đã tồn tại.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                errors.SetError(txtName, "Vui lòng nhập họ tên.");
                valid = false;
            }

            if (!Regex.IsMatch(txtPhone.Text.Trim(), @"^0[0-9]{9}$"))
            {
                errors.SetError(txtPhone, "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                errors.SetError(txtReason, "Vui lòng nhập lý do khám.");
                valid = false;
            }

            if (dtpDate.Value < DateTime.Now)
            {
                errors.SetError(dtpDate, "Ngày giờ hẹn không được ở quá khứ.");
                valid = false;
            }

            if (cboDoctor.SelectedIndex < 0)
            {
                errors.SetError(cboDoctor, "Vui lòng chọn bác sĩ.");
                valid = false;
            }

            if (cboStatus.SelectedIndex < 0)
            {
                errors.SetError(cboStatus, "Vui lòng chọn trạng thái.");
                valid = false;
            }

            return valid;
        }

        private Appointment GetInput()
        {
            return new Appointment
            {
                AppointmentId = txtId.Text.Trim(),
                PatientName = txtName.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                AppointmentDate = dtpDate.Value,
                Doctor = cboDoctor.SelectedItem?.ToString() ?? "",
                Reason = txtReason.Text.Trim(),
                Status = cboStatus.SelectedItem?.ToString() ?? "Chờ khám"
            };
        }

        private void AddAppointment()
        {
            if (!ValidateFields())
                return;

            appointments.Add(GetInput());
            RefreshData();
            ClearFields();

            MessageBox.Show("Thêm lịch hẹn thành công.");
        }

        private void UpdateAppointment()
        {
            if (selected == null)
            {
                MessageBox.Show("Hãy chọn lịch hẹn cần cập nhật.");
                return;
            }

            if (!ValidateFields())
                return;

            Appointment value = GetInput();

            selected.AppointmentId = value.AppointmentId;
            selected.PatientName = value.PatientName;
            selected.Phone = value.Phone;
            selected.AppointmentDate = value.AppointmentDate;
            selected.Doctor = value.Doctor;
            selected.Reason = value.Reason;
            selected.Status = value.Status;

            RefreshData();
            ClearFields();

            MessageBox.Show("Cập nhật lịch hẹn thành công.");
        }

        private void DeleteAppointment()
        {
            if (selected == null)
            {
                MessageBox.Show("Hãy chọn lịch hẹn cần xóa.");
                return;
            }

            if (MessageBox.Show(
                "Bạn có chắc muốn xóa lịch hẹn " + selected.AppointmentId + "?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            appointments.Remove(selected);
            RefreshData();
            ClearFields();
        }

        private void ClearFields()
        {
            selected = null;
            txtId.Clear();
            txtName.Clear();
            txtPhone.Clear();
            txtReason.Clear();

            dtpDate.Value = DateTime.Now.AddMinutes(5);
            cboDoctor.SelectedIndex = -1;
            cboStatus.SelectedItem = "Chờ khám";

            errors.Clear();
            dgv.ClearSelection();
            dgv.CurrentCell = null;
        }

        private void ExportCsv()
        {
            var visible = source.Cast<Appointment>().ToList();

            using var dialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "DanhSachLichHen.csv",
                DefaultExt = "csv",
                AddExtension = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            using var writer = new StreamWriter(
                dialog.FileName, false, new UTF8Encoding(true));

            writer.WriteLine(
                "MaLichHen,HoTen,SoDienThoai,NgayGioHen,BacSi,LyDoKham,TrangThai");

            foreach (var a in visible)
            {
                writer.WriteLine(string.Join(",",
                    Escape(a.AppointmentId),
                    Escape(a.PatientName),
                    Escape(a.Phone),
                    Escape(a.AppointmentDate.ToString("dd/MM/yyyy HH:mm")),
                    Escape(a.Doctor),
                    Escape(a.Reason),
                    Escape(a.Status)));
            }

            MessageBox.Show($"Đã xuất {visible.Count} lịch hẹn.");
        }

        private string Escape(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
