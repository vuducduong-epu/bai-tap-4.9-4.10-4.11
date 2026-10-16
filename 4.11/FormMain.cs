
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RepairServiceTracker
{
    public partial class FormMain : Form
    {
        private readonly BindingList<RepairOrder> orders = new();
        private readonly BindingSource source = new();
        private readonly ErrorProvider errors = new();

        private readonly TextBox txtId = new();
        private readonly TextBox txtCustomer = new();
        private readonly TextBox txtPhone = new();
        private readonly TextBox txtDescription = new();
        private readonly TextBox txtCost = new();
        private readonly ComboBox cboDevice = new();
        private readonly ComboBox cboStatus = new();
        private readonly DateTimePicker dtpReceived = new();
        private readonly PictureBox picDevice = new();
        private readonly Label lblImage = new();

        private readonly TextBox txtSearch = new();
        private readonly ComboBox cboFilter = new();
        private readonly DataGridView dgv = new();

        private readonly ToolStripStatusLabel lblCount = new();
        private readonly ToolStripStatusLabel lblCost = new();

        private string imagePath = "";
        private RepairOrder? selectedOrder;

        private readonly string[] statuses =
        {
            "Mới tiếp nhận",
            "Đang sửa",
            "Chờ linh kiện",
            "Hoàn tất",
            "Đã hủy"
        };

        public FormMain()
        {
            Text = "Repair Service Tracker";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1250, 820);
            MinimumSize = new Size(1000, 680);
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
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenu);
            menu.Items.Add(fileMenu);
            MainMenuStrip = menu;

            var statusBar = new StatusStrip();
            statusBar.Items.Add(lblCount);
            statusBar.Items.Add(new ToolStripStatusLabel { Spring = true });
            statusBar.Items.Add(lblCost);

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8)
            };

            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 355));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var groupInput = new GroupBox
            {
                Text = "Thông tin đơn sửa chữa",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            var input = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6
            };

            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            for (int i = 0; i < 6; i++)
                input.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66f));

            ConfigureText(txtId);
            ConfigureText(txtCustomer);
            ConfigureText(txtPhone);
            ConfigureText(txtDescription);
            ConfigureText(txtCost);

            txtDescription.Multiline = true;
            txtDescription.ScrollBars = ScrollBars.Vertical;

            ConfigureCombo(cboDevice);
            cboDevice.Items.AddRange(new object[]
            {
                "Điện thoại",
                "Laptop",
                "Máy tính để bàn",
                "Máy tính bảng",
                "Máy in",
                "Màn hình",
                "Thiết bị khác"
            });

            ConfigureCombo(cboStatus);
            cboStatus.Items.AddRange(statuses);

            dtpReceived.Format = DateTimePickerFormat.Custom;
            dtpReceived.CustomFormat = "dd/MM/yyyy";
            dtpReceived.Dock = DockStyle.Fill;

            AddField(input, "Mã đơn:", txtId, 0, 0);
            AddField(input, "Tên khách hàng:", txtCustomer, 1, 0);
            AddField(input, "Số điện thoại:", txtPhone, 0, 1);
            AddField(input, "Loại thiết bị:", cboDevice, 1, 1);
            AddField(input, "Ngày tiếp nhận:", dtpReceived, 0, 2);
            AddField(input, "Chi phí dự kiến:", txtCost, 1, 2);
            AddField(input, "Mô tả lỗi:", txtDescription, 0, 3);
            AddField(input, "Trạng thái:", cboStatus, 1, 3);

            picDevice.Dock = DockStyle.Fill;
            picDevice.BorderStyle = BorderStyle.FixedSingle;
            picDevice.SizeMode = PictureBoxSizeMode.Zoom;
            picDevice.BackColor = Color.WhiteSmoke;

            var imagePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            imagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 78));
            imagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 22));

            var btnChooseImage = new Button
            {
                Text = "Chọn ảnh thiết bị",
                Dock = DockStyle.Fill
            };
            btnChooseImage.Click += (_, _) => ChooseImage();

            imagePanel.Controls.Add(picDevice, 0, 0);
            imagePanel.Controls.Add(btnChooseImage, 0, 1);

            input.Controls.Add(new Label
            {
                Text = "Ảnh thiết bị:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 2, 4);
            input.Controls.Add(imagePanel, 3, 4);
            input.SetRowSpan(imagePanel, 2);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true
            };

            var btnAdd = new Button { Text = "Thêm", Width = 80 };
            var btnUpdate = new Button { Text = "Cập nhật", Width = 95 };
            var btnDelete = new Button { Text = "Xóa", Width = 80 };
            var btnClear = new Button { Text = "Làm mới", Width = 85 };

            btnAdd.Click += (_, _) => AddOrder();
            btnUpdate.Click += (_, _) => UpdateOrder();
            btnDelete.Click += (_, _) => DeleteOrder();
            btnClear.Click += (_, _) => ClearFields();

            buttons.Controls.AddRange(new Control[]
            {
                btnAdd, btnUpdate, btnDelete, btnClear
            });

            input.Controls.Add(buttons, 1, 5);
            input.SetColumnSpan(buttons, 2);

            groupInput.Controls.Add(input);

            var groupList = new GroupBox
            {
                Text = "Danh sách đơn sửa chữa",
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

            txtSearch.Width = 280;
            txtSearch.PlaceholderText = "Tìm mã đơn, tên khách hoặc số điện thoại";
            txtSearch.TextChanged += (_, _) => RefreshData();

            ConfigureCombo(cboFilter);
            cboFilter.Width = 150;
            cboFilter.Items.Add("Tất cả trạng thái");
            cboFilter.Items.AddRange(statuses);
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
                Text = "Lọc trạng thái:",
                AutoSize = true,
                Margin = new Padding(8, 8, 3, 3)
            });
            filterPanel.Controls.Add(cboFilter);

            ConfigureGrid();

            listLayout.Controls.Add(filterPanel, 0, 0);
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
            control.Margin = new Padding(3, 4, 8, 4);
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

            AddColumn("OrderId", "Mã đơn");
            AddColumn("CustomerName", "Khách hàng");
            AddColumn("Phone", "Số điện thoại");
            AddColumn("DeviceType", "Thiết bị");
            AddColumn("FaultDescription", "Mô tả lỗi");
            AddColumn("ReceivedDate", "Ngày tiếp nhận", "dd/MM/yyyy");
            AddColumn("EstimatedCost", "Chi phí dự kiến", "N0");
            AddColumn("Status", "Trạng thái");

            dgv.SelectionChanged += (_, _) => LoadSelectedOrder();
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
            orders.Add(new RepairOrder
            {
                OrderId = "SC001",
                CustomerName = "Nguyễn Văn An",
                Phone = "0912345678",
                DeviceType = "Laptop",
                FaultDescription = "Máy chạy chậm",
                ReceivedDate = DateTime.Today,
                EstimatedCost = 850000,
                Status = "Mới tiếp nhận"
            });

            orders.Add(new RepairOrder
            {
                OrderId = "SC002",
                CustomerName = "Trần Thị Bình",
                Phone = "0987654321",
                DeviceType = "Điện thoại",
                FaultDescription = "Màn hình bị vỡ",
                ReceivedDate = DateTime.Today.AddDays(-1),
                EstimatedCost = 1500000,
                Status = "Đang sửa"
            });

            orders.Add(new RepairOrder
            {
                OrderId = "SC003",
                CustomerName = "Lê Minh Cường",
                Phone = "0901234567",
                DeviceType = "Máy in",
                FaultDescription = "Không nhận giấy",
                ReceivedDate = DateTime.Today.AddDays(-2),
                EstimatedCost = 400000,
                Status = "Hoàn tất"
            });
        }

        private void LoadSelectedOrder()
        {
            if (dgv.CurrentRow?.DataBoundItem is not RepairOrder order)
                return;

            selectedOrder = order;

            txtId.Text = order.OrderId;
            txtCustomer.Text = order.CustomerName;
            txtPhone.Text = order.Phone;
            cboDevice.SelectedItem = order.DeviceType;
            txtDescription.Text = order.FaultDescription;
            dtpReceived.Value = order.ReceivedDate;
            txtCost.Text = order.EstimatedCost.ToString("0.##",
                CultureInfo.InvariantCulture);
            cboStatus.SelectedItem = order.Status;

            LoadDeviceImage(order.ImagePath);
        }

        private bool ValidateFields(out decimal cost)
        {
            errors.Clear();
            bool valid = true;
            cost = 0;

            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                errors.SetError(txtId, "Vui lòng nhập mã đơn.");
                valid = false;
            }
            else if (orders.Any(o =>
                o.OrderId.Equals(txtId.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase)
                && o != selectedOrder))
            {
                errors.SetError(txtId, "Mã đơn đã tồn tại.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtCustomer.Text))
            {
                errors.SetError(txtCustomer, "Vui lòng nhập tên khách hàng.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                errors.SetError(txtPhone, "Vui lòng nhập số điện thoại.");
                valid = false;
            }

            if (cboDevice.SelectedIndex < 0)
            {
                errors.SetError(cboDevice, "Vui lòng chọn loại thiết bị.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                errors.SetError(txtDescription, "Vui lòng nhập mô tả lỗi.");
                valid = false;
            }

            if (!decimal.TryParse(txtCost.Text.Trim(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture, out cost)
                && !decimal.TryParse(txtCost.Text.Trim(),
                NumberStyles.Number,
                CultureInfo.CurrentCulture, out cost))
            {
                errors.SetError(txtCost, "Chi phí phải là số hợp lệ.");
                valid = false;
            }
            else if (cost < 0)
            {
                errors.SetError(txtCost, "Chi phí không được âm.");
                valid = false;
            }

            if (cboStatus.SelectedIndex < 0)
            {
                errors.SetError(cboStatus, "Vui lòng chọn trạng thái.");
                valid = false;
            }

            return valid;
        }

        private RepairOrder ReadFields(decimal cost)
        {
            return new RepairOrder
            {
                OrderId = txtId.Text.Trim(),
                CustomerName = txtCustomer.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                DeviceType = cboDevice.SelectedItem?.ToString() ?? "",
                FaultDescription = txtDescription.Text.Trim(),
                ReceivedDate = dtpReceived.Value.Date,
                EstimatedCost = cost,
                Status = cboStatus.SelectedItem?.ToString() ?? "Mới tiếp nhận",
                ImagePath = imagePath
            };
        }

        private void AddOrder()
        {
            if (!ValidateFields(out decimal cost))
                return;

            orders.Add(ReadFields(cost));
            RefreshData();
            ClearFields();

            MessageBox.Show("Thêm đơn sửa chữa thành công.");
        }

        private void UpdateOrder()
        {
            if (selectedOrder == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần cập nhật.");
                return;
            }

            if (!ValidateFields(out decimal cost))
                return;

            RepairOrder value = ReadFields(cost);

            selectedOrder.OrderId = value.OrderId;
            selectedOrder.CustomerName = value.CustomerName;
            selectedOrder.Phone = value.Phone;
            selectedOrder.DeviceType = value.DeviceType;
            selectedOrder.FaultDescription = value.FaultDescription;
            selectedOrder.ReceivedDate = value.ReceivedDate;
            selectedOrder.EstimatedCost = value.EstimatedCost;
            selectedOrder.Status = value.Status;
            selectedOrder.ImagePath = value.ImagePath;

            RefreshData();
            ClearFields();

            MessageBox.Show("Cập nhật đơn sửa chữa thành công.");
        }

        private void DeleteOrder()
        {
            if (selectedOrder == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần xóa.");
                return;
            }

            var answer = MessageBox.Show(
                $"Bạn có chắc muốn xóa đơn {selectedOrder.OrderId}?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
                return;

            orders.Remove(selectedOrder);
            RefreshData();
            ClearFields();
        }

        private void ChooseImage()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Chọn ảnh thiết bị",
                Filter = "Tệp ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            imagePath = dialog.FileName;
            LoadDeviceImage(imagePath);
        }

        private void LoadDeviceImage(string path)
        {
            picDevice.Image = null;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var image = Image.FromStream(stream);
                picDevice.Image = new Bitmap(image);
            }
            catch
            {
                picDevice.Image = null;
                MessageBox.Show("Không thể mở ảnh đã chọn.");
            }
        }

        private void ClearFields()
        {
            selectedOrder = null;
            imagePath = "";

            txtId.Clear();
            txtCustomer.Clear();
            txtPhone.Clear();
            txtDescription.Clear();
            txtCost.Clear();

            cboDevice.SelectedIndex = -1;
            cboStatus.SelectedItem = "Mới tiếp nhận";
            dtpReceived.Value = DateTime.Today;

            picDevice.Image?.Dispose();
            picDevice.Image = null;

            errors.Clear();
            dgv.ClearSelection();
            dgv.CurrentCell = null;
        }

        private void RefreshData()
        {
            string keyword = txtSearch.Text.Trim();

            var filtered = orders
                .Where(o =>
                    o.OrderId.Contains(keyword,
                        StringComparison.OrdinalIgnoreCase)
                    || o.CustomerName.Contains(keyword,
                        StringComparison.OrdinalIgnoreCase)
                    || o.Phone.Contains(keyword,
                        StringComparison.OrdinalIgnoreCase))
                .Where(o =>
                    cboFilter.SelectedIndex <= 0
                    || o.Status == cboFilter.SelectedItem?.ToString())
                .ToList();

            source.DataSource = new BindingList<RepairOrder>(filtered);
            dgv.DataSource = source;

            lblCount.Text = $"Số đơn đang hiển thị: {filtered.Count}";

            decimal total = orders
                .Where(o => o.Status != "Hoàn tất" && o.Status != "Đã hủy")
                .Sum(o => o.EstimatedCost);

            lblCost.Text = $"Tổng chi phí dự kiến chưa hoàn tất: {total:N0} VNĐ";
        }

        private void ExportCsv()
        {
            var visible = source.Cast<RepairOrder>().ToList();

            using var dialog = new SaveFileDialog
            {
                Title = "Xuất danh sách đơn sửa chữa",
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "DanhSachDonSuaChua.csv",
                DefaultExt = "csv",
                AddExtension = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            using var writer = new StreamWriter(
                dialog.FileName, false, new UTF8Encoding(true));

            writer.WriteLine(
                "MaDon,KhachHang,SoDienThoai,LoaiThietBi,MoTaLoi,NgayTiepNhan,ChiPhiDuKien,TrangThai,DuongDanAnh");

            foreach (var o in visible)
            {
                writer.WriteLine(string.Join(",",
                    Escape(o.OrderId),
                    Escape(o.CustomerName),
                    Escape(o.Phone),
                    Escape(o.DeviceType),
                    Escape(o.FaultDescription),
                    Escape(o.ReceivedDate.ToString("dd/MM/yyyy")),
                    Escape(o.EstimatedCost.ToString("0",
                        CultureInfo.InvariantCulture)),
                    Escape(o.Status),
                    Escape(o.ImagePath)));
            }

            MessageBox.Show($"Đã xuất {visible.Count} đơn sửa chữa.");
        }

        private string Escape(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            picDevice.Image?.Dispose();
            errors.Dispose();
            base.OnFormClosed(e);
        }
    }
}
