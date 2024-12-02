using SellApp.Model;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Windows;  // Đảm bảo có import đúng thư viện này
using System.Windows.Controls;
using WpfOrientation = System.Windows.Controls.Orientation;
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
            // Tính toán lại số thứ tự (STT) cho các sản phẩm trong hóa đơn
            int index = 1;
            foreach (var item in invoiceDetails)
            {
                item.STT = index++;
            }

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
                            p.Barcode.Contains(searchText))
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


        // Phương thức tạo nội dung hóa đơn
        private StackPanel CreateInvoiceContent()
        {
            // Tạo StackPanel với chiều rộng phù hợp khổ K80
            var panel = new StackPanel
            {
                Width = 300, // Khổ giấy ~80mm (300px)
                Margin = new System.Windows.Thickness(10) // Khoảng cách nội dung
            };

            // Dòng 1: Tên cửa hàng
            TextBlock storeName = new TextBlock
            {
                Text = "CỬA HÀNG SIM KHA",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new System.Windows.Thickness(0, 5, 0, 5)
            };
            panel.Children.Add(storeName);

            // Dòng 2: HÓA ĐƠN BÁN HÀNG
            TextBlock title = new TextBlock
            {
                Text = "HÓA ĐƠN BÁN HÀNG",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new System.Windows.Thickness(0, 5, 0, 10)
            };
            panel.Children.Add(title);

            // Dòng 3: Thông tin khách hàng và ngày
            TextBlock customerInfo = new TextBlock
            {
                Text = $"Tên khách hàng: {PCUs.Text}\nNgày: {DateTime.Now:dd/MM/yyyy}",
                FontSize = 12,
                Margin = new System.Windows.Thickness(0, 5, 0, 10)
            };
            panel.Children.Add(customerInfo);

            // Dòng 4: Bảng hóa đơn
            panel.Children.Add(new TextBlock
            {
                Text = "Tên SP          SL   Đ.Giá    Thành tiền",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Margin = new System.Windows.Thickness(0, 5, 0, 5)
            });

            foreach (var item in invoiceDetails)
            {
                StackPanel row = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                row.Children.Add(new TextBlock
                {
                    Text = $"{item.ProductName}",
                    FontSize = 12,
                    Width = 120 // Cắt bớt tên dài
                });

                row.Children.Add(new TextBlock
                {
                    Text = $"{item.Quantity}",
                    FontSize = 12,
                    Width = 30,
                    TextAlignment = System.Windows.TextAlignment.Center
                });

                row.Children.Add(new TextBlock
                {
                    Text = $"{item.UnitPrice * 1000:N0}",
                    FontSize = 12,
                    Width = 60,
                    TextAlignment = System.Windows.TextAlignment.Right
                });

                row.Children.Add(new TextBlock
                {
                    Text = $"{item.TotalPrice * 1000:N0}",
                    FontSize = 12,
                    Width = 80,
                    TextAlignment = System.Windows.TextAlignment.Right
                });

                panel.Children.Add(row);
            }

            // Dòng tổng tiền
            TextBlock totalAmountText = new TextBlock
            {
                Text = $"Tổng tiền: {(invoiceDetails.Sum(i => i.TotalPrice) * 1000):N0} VND",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new System.Windows.Thickness(0, 10, 0, 5),
                TextAlignment = System.Windows.TextAlignment.Right
            };
            panel.Children.Add(totalAmountText);

            // Dòng nợ (nếu có)
            if (decimal.TryParse(PDeb.Text, out decimal debtAmount))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"Còn nợ: {(debtAmount * 1000):N0} VND",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Margin = new System.Windows.Thickness(0, 5, 0, 10),
                    TextAlignment = System.Windows.TextAlignment.Right
                });
            }

            return panel;
        }


        // Phương thức in hóa đơn
        private void btnPrint(object sender, RoutedEventArgs e)
        {
            // Tạo nội dung hóa đơn
            var printContent = CreateInvoiceContent();

            // Hiển thị hộp thoại in
            PrintDialog printDialog = new PrintDialog();
            printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(80, 200); // Khổ K80, dài tùy hóa đơn

            if (printDialog.ShowDialog() == true)
            {
                // In trực tiếp
                printDialog.PrintVisual(printContent, "Hóa đơn bán hàng");
            }
        }




    }
}
