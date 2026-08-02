using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;

namespace EnterpriseBillingSystem.Wpf.Views.Purchases
{
    public class PurchaseReceiptItemDisplayModel
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal => Quantity * UnitPrice;
    }

    public partial class PurchaseReceiptDetailDialog : Window, INotifyPropertyChanged
    {
        private string _receiptNumber = string.Empty;
        private string _supplierName = string.Empty;
        private string _warehouseName = string.Empty;
        private string _referenceDocument = string.Empty;
        private DateTime _receiptDate;
        private string _notes = string.Empty;

        public string ReceiptNumber
        {
            get => _receiptNumber;
            set { _receiptNumber = value; OnPropertyChanged(); }
        }

        public string SupplierName
        {
            get => _supplierName;
            set { _supplierName = value; OnPropertyChanged(); }
        }

        public string WarehouseName
        {
            get => _warehouseName;
            set { _warehouseName = value; OnPropertyChanged(); }
        }

        public string ReferenceDocument
        {
            get => _referenceDocument;
            set { _referenceDocument = value; OnPropertyChanged(); }
        }

        public DateTime ReceiptDate
        {
            get => _receiptDate;
            set { _receiptDate = value; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PurchaseReceiptItemDisplayModel> Details { get; } = new();

        public decimal TotalQuantity => Details.Sum(d => d.Quantity);
        public decimal TotalCost => Details.Sum(d => d.SubTotal);

        public string TotalQuantityDisplay => $"{TotalQuantity:N2} pzas";
        public string TotalCostDisplay => $"{TotalCost:C2}";

        public PurchaseReceiptDetailDialog(PurchaseReceiptDetailDto dto)
        {
            InitializeComponent();
            DataContext = this;

            ReceiptNumber = dto.ReceiptNumber;
            SupplierName = dto.SupplierName;
            WarehouseName = string.IsNullOrWhiteSpace(dto.WarehouseName) ? "Bodega General" : dto.WarehouseName;
            ReferenceDocument = string.IsNullOrWhiteSpace(dto.ReferenceDocument) ? "N/A" : dto.ReferenceDocument;
            ReceiptDate = dto.ReceiptDate;
            Notes = dto.Notes ?? string.Empty;

            if (dto.Details != null)
            {
                foreach (var item in dto.Details)
                {
                    Details.Add(new PurchaseReceiptItemDisplayModel
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        ProductCode = item.ProductCode,
                        UnitOfMeasure = item.UnitOfMeasure,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }
            }

            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(TotalCost));
            OnPropertyChanged(nameof(TotalQuantityDisplay));
            OnPropertyChanged(nameof(TotalCostDisplay));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
