using SellApp.Model;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace SellApp
{
    public partial class MainWindow : Window
    {
        public string Debt { get; set; } = "0";

        private List<Product> products = new List<Product>();
        private List<OrderDetail> orderDetails = new List<OrderDetail>(); // Danh sách chi tiết đơn hàng
        private SellappContext myContext = new SellappContext();

        private ObservableCollection<OrderDetail> invoiceDetails = new ObservableCollection<OrderDetail>();

        public MainWindow()
        {
            InitializeComponent();
            LoadProducts();
        }

        private void LoadProducts()
        {
            if (dgData == null) return;

            var productData = myContext.Products
                .Select(s => new
                {
                    s.ProductId,
                    s.ProductName,
                    s.Barcode,
                    s.Unit,
                    s.Price,
                    s.Note,
                })
                .ToList();

            dgData.ItemsSource = productData;
        }

        private void dgData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProduct = dgData.SelectedItem;
            if (selectedProduct == null) return;

            dynamic product = selectedProduct;

            // Kiểm tra xem sản phẩm đã có trong hóa đơn chưa
            var existingItem = invoiceDetails.FirstOrDefault(p => p.ProductId == product.ProductId);
            if (existingItem != null)
            {
                // Nếu sản phẩm đã tồn tại, tăng số lượng
                existingItem.Quantity++;
            }
            else
            {
                // Nếu chưa tồn tại, thêm sản phẩm mới vào hóa đơn
                var newItem = new OrderDetail
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    UnitBill = product.Unit,
                    UnitPrice = product.Price,
                    Quantity = 1
                };
                invoiceDetails.Add(newItem);
            }

            // Cập nhật DataGrid hóa đơn bên phải
            RefreshInvoiceGrid();

            // Cập nhật tổng tiền ngay lập tức sau khi thay đổi
            UpdateTotalAmount();
        }


        private void RefreshInvoiceGrid()
        {
            // Không cần tái tạo ObservableCollection, chỉ cần gán trực tiếp
            dgHoaDon.ItemsSource = null;
            dgHoaDon.ItemsSource = invoiceDetails;
        }


        // Phương thức tăng số lượng
        private void IncreaseQuantity(int productId)
        {
            var product = invoiceDetails.FirstOrDefault(p => p.ProductId == productId);
            if (product != null)
            {
                product.Quantity++;
            }

            // Cập nhật lại DataGrid và tổng tiền
            RefreshInvoiceGrid();
            UpdateTotalAmount();
        }

        // Phương thức giảm số lượng
        private void DecreaseQuantity(int productId)
        {
            var product = invoiceDetails.FirstOrDefault(p => p.ProductId == productId);
            if (product != null && product.Quantity > 1)
            {
                product.Quantity--;
            }
            else if (product != null && product.Quantity == 1)
            {
                // Nếu số lượng bằng 1, xóa sản phẩm khỏi hóa đơn
                invoiceDetails.Remove(product);
            }

            // Cập nhật lại DataGrid và tổng tiền
            RefreshInvoiceGrid();
            UpdateTotalAmount();
        }

        // Cập nhật tổng tiền
        private void UpdateTotalAmount()
        {
            decimal totalAmount = invoiceDetails.Sum(item => item.TotalPrice);
            lblTotalAmount.Content = $"{totalAmount * 1000:#,##0} VND";

        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var productId = (int)button.Tag;
                IncreaseQuantity(productId);
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var productId = (int)button.Tag;
                DecreaseQuantity(productId);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (myContext.Products == null || !myContext.Products.Any())
            {
                MessageBox.Show("Không có dữ liệu sản phẩm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var searchText = txtSearch.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(searchText) || searchText == "tìm kiếm sản phẩm ...")
            {
                LoadProducts();
                return;
            }

            var filteredProducts = myContext.Products
                .Where(p => p.ProductName.ToLower().Contains(searchText) ||
                            p.Barcode.ToLower().Contains(searchText))
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Barcode,
                    p.Unit,
                    p.Price,
                    p.Note
                })
                .ToList();
            // Kiểm tra kết quả lọc
            if (!filteredProducts.Any())
            {
                return;
            }
            dgData.ItemsSource = filteredProducts;
        }

        private void RemovePlaceholder(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null || tb.Text != "Tìm kiếm sản phẩm ...") return;

            tb.Text = "";
            tb.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void AddPlaceholder(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null || !string.IsNullOrWhiteSpace(tb.Text)) return;

            tb.Text = "Tìm kiếm sản phẩm ...";
            tb.Foreground = new SolidColorBrush(Colors.Gray);
        }

        private void btnSell(object sender, RoutedEventArgs e)
        {
            MainWindow sell = new MainWindow();
            sell.Show();
            this.Close();
        }

        private void btnPro(object sender, RoutedEventArgs e)
        {
            ProductWindow product = new ProductWindow();
            product.Show();
            this.Close();
        }

        private void btnCom(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Xác nhận hoàn thành đơn hàng?", "Xác nhận", MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
            {
                // Xóa danh sách hóa đơn
                invoiceDetails.Clear();

                // Cập nhật lại DataGrid và tổng tiền
                UpdateTotalAmount();

                MessageBox.Show("Đơn hàng đã được hoàn thành!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Người dùng chọn "Không", không làm gì
            }
        }

        private void PDeb_TextChanged(object sender, TextChangedEventArgs e)
        {
            Debt = PDeb.Text;
        }

        private void PDeb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(PDeb.Text, out decimal debtAmount))
            {
                PDeb.Text = $"{debtAmount:N0}";
            }
        }



        private void btnPrint(object sender, RoutedEventArgs e)
        {
            if (invoiceDetails.Count == 0) // Kiểm tra danh sách có dữ liệu không
            {
                MessageBox.Show("Không có sản phẩm nào trong hóa đơn để in.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Tạo workbook và worksheet
            var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Hóa đơn");

            // Tiêu đề - Cửa hàng và hóa đơn
            worksheet.Cell(1, 1).Value = "CỬA HÀNG SIM KHA";
            worksheet.Cell(1, 1).Style.Font.SetBold(true).Font.SetFontSize(16);
            worksheet.Cell(1, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            worksheet.Cell(1, 1).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top);

            worksheet.Cell(1, 5).Value = "HÓA ĐƠN BÁN HÀNG";
            worksheet.Cell(1, 5).Style.Font.SetBold(true).Font.SetFontSize(16);
            worksheet.Cell(1, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            worksheet.Cell(1, 5).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top);

            // Địa chỉ và số điện thoại
            worksheet.Cell(3, 1).Value = "Địa chỉ: Xóm 2 An Tiến - Chí Hòa - Hưng Hà - Thái Bình";
            worksheet.Cell(3, 1).Style.Font.SetBold(true).Font.SetFontSize(12);
            worksheet.Cell(3, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            worksheet.Cell(4, 1).Value = "SDT: 0973835155";
            worksheet.Cell(4, 1).Style.Font.SetBold(true).Font.SetFontSize(12);
            worksheet.Cell(4, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            // Tên khách hàng nếu có
            if (!string.IsNullOrEmpty(PCUs.Text))
            {
                worksheet.Cell(5, 1).Value = "Tên khách hàng: " + PCUs.Text;
                worksheet.Cell(5, 1).Style.Font.SetFontSize(12);
                worksheet.Cell(5, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            }

            // Dữ liệu bảng hóa đơn
            int row = 7; // Dữ liệu bắt đầu từ dòng 7
            worksheet.Cell(row, 1).Value = "Tên sản phẩm";
            worksheet.Cell(row, 2).Value = "Đơn vị";
            worksheet.Cell(row, 3).Value = "Số lượng";
            worksheet.Cell(row, 4).Value = "Đơn giá";
            worksheet.Cell(row, 5).Value = "Thành tiền";

            // Định dạng tiêu đề bảng (căn giữa và in đậm)
            worksheet.Range("A7:E7").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            worksheet.Range("A7:E7").Style.Font.SetBold(true);
            worksheet.Range("A7:E7").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range("A7:E7").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Thêm viền cho các ô dữ liệu
            worksheet.Columns("A:E").AdjustToContents(); // Tự động điều chỉnh cột theo nội dung

            // Thêm dữ liệu vào bảng hóa đơn
            foreach (var item in invoiceDetails)
            {
                row++;
                worksheet.Cell(row, 1).Value = item.ProductName;
                worksheet.Cell(row, 2).Value = item.UnitBill;
                worksheet.Cell(row, 3).Value = item.Quantity;
                worksheet.Cell(row, 4).Value = item.UnitPrice * 1000; // Nhân giá tiền với 1000
                worksheet.Cell(row, 5).Value = item.TotalPrice * 1000; // Nhân thành tiền với 1000

                // Định dạng viền cho từng dòng dữ liệu
                worksheet.Range($"A{row}:E{row}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{row}:E{row}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // In tổng tiền
            row++;
            worksheet.Cell(row, 4).Value = "Tổng tiền:";
            worksheet.Cell(row, 5).Value = $"{invoiceDetails.Sum(item => item.TotalPrice) * 1000:#,##0} VND";
            worksheet.Cell(row, 4).Style.Font.SetBold(true).Font.SetFontSize(14);
            worksheet.Cell(row, 5).Style.Font.SetBold(true).Font.SetFontSize(14);
            worksheet.Range($"D{row}:E{row}").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Kiểm tra xem có nợ không, nếu có thì thêm "Còn nợ"
            if (decimal.TryParse(PDeb.Text, out decimal debtAmount) && debtAmount > 0)
            {
                row++;
                worksheet.Cell(row, 4).Value = "Còn nợ:";
                worksheet.Cell(row, 5).Value = $"{debtAmount * 1000:#,##0} VND"; // Định dạng nợ theo định dạng VND
                worksheet.Cell(row, 4).Style.Font.SetBold(true).Font.SetFontSize(14);
                worksheet.Cell(row, 5).Style.Font.SetBold(true).Font.SetFontSize(14);
                worksheet.Range($"D{row}:E{row}").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            // Ngày tháng năm
            worksheet.Cell(row + 1, 5).Value = "Ngày: " + System.DateTime.Now.ToString("dd/MM/yyyy");
            worksheet.Cell(row + 1, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            worksheet.Cell(row + 1, 5).Style.Font.SetFontSize(12);

            // Lưu file Excel
            // Tính tổng tiền hóa đơn
            decimal totalAmount = invoiceDetails.Sum(item => item.TotalPrice);

            // Gọi phương thức để tạo tên file
            var fileName = FormatFileName(PCUs.Text, totalAmount); // Truyền cả tên khách hàng và tổng tiền vào phương thức

            // Lưu file Excel
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = fileName // Sử dụng tên file vừa tạo
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                workbook.SaveAs(saveFileDialog.FileName); // Lưu file với tên đã tạo
            }

            // Điều chỉnh cột D (Tổng tiền) theo chiều rộng nội dung
            worksheet.Column(4).AdjustToContents();
            worksheet.Column(4).Width = 30; // Thử set chiều rộng cột "Tổng tiền"

            // Căn chỉnh lại cột Tổng tiền (cột 4 và cột 5)
            worksheet.Cell(row, 4).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center); // Căn chỉnh dọc
            worksheet.Cell(row, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center); // Căn chỉnh ngang

            // Điều chỉnh lại tất cả các cột nếu cần thiết
            worksheet.Columns("A:E").AdjustToContents();
        }


        // Hàm xử lý tên khách hàng để loại bỏ khoảng trắng và ký tự đặc biệt
        private string FormatFileName(string customerName, decimal totalAmount)
        {
            // Nếu không có tên khách hàng, chỉ sử dụng tổng tiền
            if (string.IsNullOrEmpty(customerName))
                return $"{totalAmount:#,##0},000VND-HoaDonBanHang"; // Chỉ có tổng tiền và HoaDonBanHang

            // Nếu có tên khách hàng, tách tên thành các từ và viết hoa chữ đầu tiên của mỗi từ
            var words = customerName.Split(' ');
            var formattedName = string.Join("", words.Select(word =>
                char.ToUpper(word[0]) + word.Substring(1).ToLower())); // Viết hoa chữ cái đầu

            // Trả về tên file theo định dạng: Tên khách hàng - Tổng tiền - HoaDonBanHang
            return $"{formattedName}-{totalAmount:#,##0},000VND-HoaDonBanHang";
        }




    }
}
