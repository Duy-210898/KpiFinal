using KpiApplication.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KpiApplication.Models
{
    public class IEPPHDataForUser_Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
         
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
#if DEBUG
            try
            {
                var value = GetType().GetProperty(propertyName)?.GetValue(this);
                Console.WriteLine($"🔄 Property changed: {propertyName} => {value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi log thay đổi property {propertyName}: {ex.Message}");
            }
#endif
        }

        public bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Properties
        private string _articleName;
        public string ArticleName { get => _articleName; set => SetProperty(ref _articleName, value); }

        private string _modelName;
        public string ModelName { get => _modelName; set => SetProperty(ref _modelName, value); }

        private string _pcSend;
        public string PCSend { get => _pcSend; set => SetProperty(ref _pcSend, value); }

        private string _personIncharge; 
        public string PersonIncharge { get => _personIncharge; set => SetProperty(ref _personIncharge, value); }

        private string _noteForPC;
        public string NoteForPC { get => _noteForPC; set => SetProperty(ref _noteForPC, value); }
        public string OutsourcingAssembling => OutsourcingAssemblingBool ? "YES" : "";

        public string OutsourcingStitching => OutsourcingStitchingBool ? "YES" : "";

        public string OutsourcingStockFitting => OutsourcingStockFittingBool ? "YES" : "";

        private bool _outsourcingAssemblingBool;
        public bool OutsourcingAssemblingBool { get => _outsourcingAssemblingBool; set => SetProperty(ref _outsourcingAssemblingBool, value); }

        private bool _outsourcingStitchingBool;
        public bool OutsourcingStitchingBool { get => _outsourcingStitchingBool; set => SetProperty(ref _outsourcingStitchingBool, value); }

        private bool _outsourcingStockFittingBool;
        public bool OutsourcingStockFittingBool { get => _outsourcingStockFittingBool; set => SetProperty(ref _outsourcingStockFittingBool, value); }

        private string _dataStatus;
        public string DataStatus { get => _dataStatus; set => SetProperty(ref _dataStatus, value); }

        private string _process;
        public string Process { get => _process; set => SetProperty(ref _process, value); }

        private int? _targetOutputPC;
        public int? TargetOutputPC { get => _targetOutputPC; set => SetProperty(ref _targetOutputPC, value); }

        private int? _adjustOperatorNo;
        public int? AdjustOperatorNo { get => _adjustOperatorNo; set => SetProperty(ref _adjustOperatorNo, value); }

        private double? _iePPHValue;
        public double? IEPPHValue 
        { 
            get => _iePPHValue; 
            set
            {
                if (SetProperty(ref _iePPHValue, value))
                    CalculateTHT();
            }

        }

        private double? _thtValue;
        public double? THTValue { get => _thtValue; set => SetProperty(ref _thtValue, value); }

        private string _typeName;
        public string TypeName { get => _typeName; set => SetProperty(ref _typeName, value); }
        private void CalculateTHT()
        {
            THTValue = (IEPPHValue.HasValue)
                ? (double?)Math.Round(3600 / IEPPHValue.Value, 0)
                : null;
        }


        private string isSigned;
        public string IsSigned { get => isSigned; set => SetProperty(ref isSigned, value); }
    }
}
