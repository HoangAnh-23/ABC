using Microsoft.EntityFrameworkCore;
using SellApp.Model;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using SellApp;

namespace SellApp
{
    public partial class ProductWindow : Window
    {
        private List<Product> products = new List<Product>();
        SellappContext myContext = new SellappContext();

        public ProductWindow()
        {
            InitializeComponent();
            LoadProducts();
        }

        private void LoadProducts()
        {
            if (dgData == null) return;

            // Sử dụng trực tiếp class Product
            var productData = myContext.Products.ToList();
            dgData.ItemsSource = productData;
        }


        private void dgData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgData.SelectedItem is Product selectedProduct)
            {
                PID.Text = selectedProduct.ProductId.ToString();
                PName.Text = selectedProduct.ProductName;
                PCode.Text = selectedProduct.Barcode ?? string.Empty;
                PUnit.Text = selectedProduct.Unit ?? string.Empty;
                PPrice.Text = string.Format("{0:N0}", selectedProduct.Price);
                PDetail.Text = selectedProduct.Note ?? string.Empty;

                PID.Foreground = new SolidColorBrush(Colors.Black);
                PName.Foreground = new SolidColorBrush(Colors.Black);
                PCode.Foreground = new SolidColorBrush(Colors.Black);
                PUnit.Foreground = new SolidColorBrush(Colors.Black);
                PPrice.Foreground = new SolidColorBrush(Colors.Black);
                PDetail.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                // Reset các TextBox
                PID.Text = "ID";
                PName.Text = "Tên sản phẩm";
                PCode.Text = "Mã vạch";
                PUnit.Text = "Đơn vị";
                PPrice.Text = "Giá Tiền";
                PDetail.Text = "Ghi chú";

                AddPlaceholder(PID, null);
                AddPlaceholder(PName, null);
                AddPlaceholder(PCode, null);
                AddPlaceholder(PUnit, null);
                AddPlaceholder(PPrice, null);
                AddPlaceholder(PDetail, null);
            }
        }


        private void btnAdd(object sender, RoutedEventArgs e)
        {
            // Lấy giá trị từ các TextBox, kiểm tra và loại bỏ placeholder
            string productName = PName.Text.Trim();
            string barcode = PCode.Text.Trim() == "Mã vạch" ? null : PCode.Text.Trim();
            string unit = PUnit.Text.Trim() == "Đơn vị" ? null : PUnit.Text.Trim();
            decimal price;
            string notes = PDetail.Text.Trim() == "Ghi chú" ? null : PDetail.Text.Trim();

            // Kiểm tra bắt buộc ProductName và Price không được để trống
            if (string.IsNullOrEmpty(productName))
            {
                MessageBox.Show("Tên sản phẩm không được để trống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(PPrice.Text) || !decimal.TryParse(PPrice.Text, out price))
            {
                MessageBox.Show("Giá tiền không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Tạo một đối tượng sản phẩm mới
            var newProduct = new Product
            {
                ProductName = productName,
                Barcode = barcode,
                Unit = unit,
                Price = price,
                Note = notes
            };

            // Thêm sản phẩm vào cơ sở dữ liệu
            myContext.Products.Add(newProduct);
            myContext.SaveChanges();

            // Tải lại danh sách sản phẩm để hiển thị trên DataGrid
            LoadProducts();

            // Xóa các TextBox sau khi thêm sản phẩm
            ResetInputFields();

            MessageBox.Show("Sản phẩm đã được thêm thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ResetInputFields()
        {
            PID.Text = "";
            PName.Text = "Tên sản phẩm";
            PCode.Text = "Mã vạch";
            PUnit.Text = "Đơn vị";
            PPrice.Text = "Giá Tiền";
            PDetail.Text = "Ghi chú";

            // Đặt màu chữ placeholder
            PID.Foreground = new SolidColorBrush(Colors.Gray);
            PName.Foreground = new SolidColorBrush(Colors.Gray);
            PCode.Foreground = new SolidColorBrush(Colors.Gray);
            PUnit.Foreground = new SolidColorBrush(Colors.Gray);
            PPrice.Foreground = new SolidColorBrush(Colors.Gray);
            PDetail.Foreground = new SolidColorBrush(Colors.Gray);
        }



        private void btnEdit(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem đã chọn sản phẩm để sửa chưa
            if (string.IsNullOrEmpty(PID.Text))
            {
                MessageBox.Show("Vui lòng chọn sản phẩm cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Chuyển đổi ProductId từ chuỗi sang số nguyên
            if (!int.TryParse(PID.Text, out int productId))
            {
                MessageBox.Show("ID sản phẩm không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Tìm sản phẩm trong cơ sở dữ liệu
            var existingProduct = myContext.Products.FirstOrDefault(p => p.ProductId == productId);
            if (existingProduct == null)
            {
                MessageBox.Show("Không tìm thấy sản phẩm cần sửa.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Lấy dữ liệu từ các TextBox
            string productName = PName.Text.Trim();
            string barcode = PCode.Text.Trim();
            string unit = PUnit.Text.Trim();
            string notes = PDetail.Text.Trim();
            decimal price;

            // Kiểm tra tính hợp lệ của dữ liệu
            if (string.IsNullOrEmpty(productName))
            {
                MessageBox.Show("Tên sản phẩm không được để trống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PPrice.Text, out price))
            {
                MessageBox.Show("Giá sản phẩm không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Cập nhật thông tin sản phẩm
            existingProduct.ProductName = productName;
            existingProduct.Barcode = barcode;
            existingProduct.Unit = unit;
            existingProduct.Price = price;
            existingProduct.Note = notes;

            // Lưu thay đổi vào cơ sở dữ liệu
            myContext.SaveChanges();

            // Tải lại danh sách sản phẩm trên DataGrid
            LoadProducts();

            // Xóa các TextBox sau khi sửa sản phẩm
            PID.Text = "";
            PName.Text = "";
            PCode.Text = "";
            PUnit.Text = "";
            PPrice.Text = "";
            PDetail.Text = "";

            AddPlaceholder(PID, null);
            AddPlaceholder(PName, null);
            AddPlaceholder(PCode, null);
            AddPlaceholder(PUnit, null);
            AddPlaceholder(PPrice, null);
            AddPlaceholder(PDetail, null);

            MessageBox.Show("Sửa sản phẩm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnDelete(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem đã chọn sản phẩm để xóa chưa
            if (string.IsNullOrEmpty(PID.Text))
            {
                MessageBox.Show("Vui lòng chọn sản phẩm cần xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Chuyển đổi ProductId từ chuỗi sang số nguyên
            if (!int.TryParse(PID.Text, out int productId))
            {
                MessageBox.Show("ID sản phẩm không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Xác nhận việc xóa sản phẩm
            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa sản phẩm này không?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // Tìm sản phẩm trong cơ sở dữ liệu
            var productToDelete = myContext.Products.FirstOrDefault(p => p.ProductId == productId);
            if (productToDelete == null)
            {
                MessageBox.Show("Không tìm thấy sản phẩm cần xóa.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Xóa sản phẩm khỏi cơ sở dữ liệu
            myContext.Products.Remove(productToDelete);
            myContext.SaveChanges();

            // Tải lại danh sách sản phẩm trên DataGrid
            LoadProducts();

            // Xóa các TextBox sau khi xóa sản phẩm
            PID.Text = "";
            PName.Text = "";
            PCode.Text = "";
            PUnit.Text = "";
            PPrice.Text = "";
            PDetail.Text = "";

            AddPlaceholder(PID, null);
            AddPlaceholder(PName, null);
            AddPlaceholder(PCode, null);
            AddPlaceholder(PUnit, null);
            AddPlaceholder(PPrice, null);
            AddPlaceholder(PDetail, null);

            MessageBox.Show("Xóa sản phẩm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void RemovePlaceholder(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text == "ID" || tb.Text == "Tên sản phẩm" || tb.Text == "Mã vạch" || tb.Text == "Đơn vị" || tb.Text == "Giá Tiền" || tb.Text == "Ghi chú" || tb.Text == "Tìm kiếm sản phẩm ...")
            {
                tb.Text = "";
                tb.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void AddPlaceholder(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                if (tb.Name == "PID")
                    tb.Text = "ID";
                else if (tb.Name == "PName")
                    tb.Text = "Tên sản phẩm";
                else if (tb.Name == "PCode")
                    tb.Text = "Mã vạch";
                else if (tb.Name == "PUnit")
                    tb.Text = "Đơn vị";
                else if (tb.Name == "PPrice")
                    tb.Text = "Giá Tiền";
                else if (tb.Name == "PDetail")
                    tb.Text = "Ghi chú";
                else if (tb.Name == "txtSearch")
                    tb.Text = "Tìm kiếm sản phẩm ...";

                tb.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Kiểm tra null hoặc trường hợp không có sản phẩm trong danh sách
            if (myContext.Products == null || !myContext.Products.Any())
            {
                MessageBox.Show("Không có dữ liệu sản phẩm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var searchText = txtSearch.Text.ToLower().Trim();

            if (string.IsNullOrEmpty(searchText) || searchText == "tìm kiếm sản phẩm ...")
            {
                // Nếu ô tìm kiếm trống, tải lại toàn bộ sản phẩm
                LoadProducts();
                return;
            }

            // Lọc sản phẩm dựa trên văn bản tìm kiếm
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

            // Cập nhật DataGrid với kết quả tìm kiếm
            dgData.ItemsSource = filteredProducts;
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

    }
}

