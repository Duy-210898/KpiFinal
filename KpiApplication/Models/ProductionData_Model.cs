using KpiApplication.DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace KpiApplication.Models
{
    public class ProductionData_Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ProductionData_Model()
        {
            ScanDate = DateTime.Today;
            IsVisible = true;

        }

        public ProductionData_Model Clone() => (ProductionData_Model)this.MemberwiseClone();

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (!string.IsNullOrEmpty(propertyName))
            {
                try
                {
                    var property = GetType().GetProperty(propertyName);
                    if (property != null)
                    {
                        var value = property.GetValue(this);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Lỗi khi log thay đổi property {propertyName}: {ex.Message}");
                }
            }
        }

        public Action<ProductionData_Model, string> OnMergeStatusChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);

            if (propertyName == nameof(IsMerged) || propertyName == nameof(MergeGroupID))
            {
                OnMergeStatusChanged?.Invoke(this, propertyName);
            }
            return true;
        }

        private static List<string> _slidesKeywords;
        private static List<string> SlidesKeywords
        {
            get
            {
                if (_slidesKeywords == null)
                {
                    try
                    {
                        _slidesKeywords = ProductionData_DAL
                            .GetAllSlidesModels()
                            .Select(m => m.SlidesModelName)
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Lỗi khi load slidesKeywords: {ex.Message}");
                        _slidesKeywords = new List<string>();
                    }
                }
                return _slidesKeywords;
            }
        }

        // Identifiers
        private int _productionId;
        public int ProductionID
        {
            get => _productionId;
            set => SetProperty(ref _productionId, value);
        }

        private int? _articleId;
        public int? ArticleID
        {
            get => _articleId;
            set => SetProperty(ref _articleId, value);
        }

        private DateTime? _scanDate;
        public DateTime? ScanDate
        {
            get => _scanDate;
            set => SetProperty(ref _scanDate, value);
        }

        private string _departmentCode;
        public string DepartmentCode
        {
            get => _departmentCode;
            set => SetProperty(ref _departmentCode, value);
        }

        private string _process;
        public string Process
        {
            get => _process;
            set => SetProperty(ref _process, value);
        }

        private string _factory;
        public string Factory
        {
            get => _factory;
            set => SetProperty(ref _factory, value);
        }

        private string _plant;
        public string Plant
        {
            get => _plant;
            set => SetProperty(ref _plant, value);
        }

        private string _lineName;
        public string LineName
        {
            get => _lineName;
            set => SetProperty(ref _lineName, value);
        }

        private string _modelName;
        public string ModelName
        {
            get => _modelName;
            set
            {
                if (SetProperty(ref _modelName, value))
                {
                    IsSlides = SlidesKeywords.Any(keyword =>
                        !string.IsNullOrEmpty(_modelName) &&
                        _modelName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!IsMerged)
                    {
                        LargestOutput = _modelName;
                    }
                }
            }
        }

        private string _article;
        public string Article
        {
            get => _article;
            set => SetProperty(ref _article, value);
        }

        private int? _quantity;
        public int? Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                    Recalculate();
            }
        }
        private int? _targetIE;
        public int? TargetIE
        {
            get => _targetIE;
            set => SetProperty(ref _targetIE, value);
        }

        private int? _totalWorker;
        public int? TotalWorker
        {
            get => _totalWorker;
            set
            {
                if (SetProperty(ref _totalWorker, value))
                    Recalculate();
            }
        }

        private double? _workingTime;
        public double? WorkingTime
        {
            get => _workingTime;
            set
            {
                if (SetProperty(ref _workingTime, value))
                    Recalculate();
            }
        }

        private double? _totalWorkingHours;
        public double? TotalWorkingHours
        {
            get => _totalWorkingHours;
            set => SetProperty(ref _totalWorkingHours, value);
        }

        private int? _targetOfPC;
        public int? TargetOfPC
        {
            get => _targetOfPC;
            set
            {
                if (SetProperty(ref _targetOfPC, value))
                    CalculateIEPPH();  // Gọi tính toán lại
            }
        }

        private int? _operatorAdjust;
        public int? OperatorAdjust
        {
            get => _operatorAdjust;
            set
            {
                if (SetProperty(ref _operatorAdjust, value))
                    CalculateIEPPH();  // Gọi tính toán lại
            }
        }

        private double? _iePPH;
        public double? IEPPH
        {
            get => _iePPH;
            set => SetProperty(ref _iePPH, value);
        }

        // 🆕 Hàm tính toán tự động
        private void CalculateIEPPH()
        {
            if (TargetOfPC.HasValue && OperatorAdjust.HasValue &&
                OperatorAdjust.Value != 0 && TargetOfPC.Value != 0)
            {
                IEPPH = Math.Round((double)TargetOfPC.Value / OperatorAdjust.Value, 2);
            }
            else
            {
                IEPPH = null;
            }
        }

        private string _typeName;
        public string TypeName
        {
            get => _typeName;
            set => SetProperty(ref _typeName, value);
        }

        private double? _outputRateValue;
        public double? OutputRateValue
        {
            get => _outputRateValue;
            set
            {
                if (SetProperty(ref _outputRateValue, value))
                    OnPropertyChanged(nameof(OutputRate));
            }
        }

        private double? _pphRateValue;
        public double? PPHRateValue
        {
            get => _pphRateValue;
            set
            {
                if (SetProperty(ref _pphRateValue, value))
                    OnPropertyChanged(nameof(PPHRate));
            }
        }

        private double? _actualPPH;
        public double? ActualPPH
        {
            get => _actualPPH;
            set
            {
                if (SetProperty(ref _actualPPH, value))
                    OnPropertyChanged(nameof(ActualPPH));
            }
        }

        public string OutputRate => OutputRateValue.HasValue
            ? $"{(int)(OutputRateValue.Value * 1000) / 10.0:0.0}%"
            : string.Empty;

        public string PPHRate => PPHRateValue.HasValue
            ? $"{Math.Round(PPHRateValue.Value * 100, 1):0.0}%"
            : string.Empty;

        private bool _isMerged;
        public bool IsMerged
        {
            get => _isMerged;
            set => SetProperty(ref _isMerged, value);
        }
        public void SetMergeInfoSilently(bool isMerged, int groupId, bool isVisible)
        {
            _isMerged = isMerged;
            _mergeGroupID = groupId;
            _isVisible = isVisible;
        }

        private int? _mergeGroupID;
        public int? MergeGroupID
        {
            get => _mergeGroupID;
            set => SetProperty(ref _mergeGroupID, value);
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        private string _largestOutput;
        public string LargestOutput
        {
            get => _largestOutput;
            set => SetProperty(ref _largestOutput, value);
        }

        private string _pphFallsBelowReasons;
        public string PPHFallsBelowReasons
        {
            get => _pphFallsBelowReasons;
            set => SetProperty(ref _pphFallsBelowReasons, value);
        }

        public void Recalculate()
        {
            CalculateTarget();
            CalculateTotalWorkingHours();
            CalculateOutputRate();
            CalculateActualPPH();
            CalculatePPHRate();
        }

        private void CalculateTarget()
        {
            if (TotalWorker.HasValue && WorkingTime.HasValue && IEPPH.HasValue &&
                TotalWorker.Value > 0 && WorkingTime.Value > 0 && IEPPH.Value > 0)
            {
                TargetOfPC = (int?)Math.Round(TotalWorker.Value * WorkingTime.Value * IEPPH.Value, 1);
            }
            else
            {
                TargetOfPC = null;
            }
        }


        private void CalculateActualPPH()
        {
            if (Quantity.HasValue && TotalWorker.HasValue && WorkingTime.HasValue &&
                Quantity.Value > 0 && TotalWorker.Value > 0 && WorkingTime.Value > 0)
            {
                ActualPPH = Math.Round(Quantity.Value / WorkingTime.Value / TotalWorker.Value, 1);
            }
            else
            {
                ActualPPH = null;
            }
        }
        private void CalculateTotalWorkingHours()
        {
            if (TotalWorker.HasValue && WorkingTime.HasValue &&
                TotalWorker.Value > 0 && WorkingTime.Value > 0)
            {
                TotalWorkingHours = Math.Round(TotalWorker.Value * WorkingTime.Value, 1);
            }
            else
            {
                TotalWorkingHours = null;
            }
        }
        public void CalculateOutputRate()
        {
            if (Quantity.HasValue && TargetOfPC.HasValue &&
                Quantity.Value > 0 && TargetOfPC.Value > 0)
            {
                OutputRateValue = Math.Round((double)Quantity.Value / TargetOfPC.Value, 4);
            }
            else
            {
                OutputRateValue = null;
            }
        }

        private void CalculatePPHRate()
        {
            if (IEPPH.HasValue && ActualPPH.HasValue &&
                IEPPH.Value > 0 && ActualPPH.Value > 0)
            {
                PPHRateValue = ActualPPH.Value / IEPPH.Value; 
            }
            else
            {
                PPHRateValue = null;
            }
        }

        private bool _isSlides;
        public bool IsSlides
        {
            get => _isSlides;
            private set => SetProperty(ref _isSlides, value);
        }

        public void CopyFrom(ProductionData_Model source)
        {
            if (source == null) return;

            ProductionID = source.ProductionID;
            ArticleID = source.ArticleID;
            ScanDate = source.ScanDate;
            DepartmentCode = source.DepartmentCode;
            Process = source.Process;
            Factory = source.Factory;
            Plant = source.Plant;
            LineName = source.LineName;
            ModelName = source.ModelName;
            Article = source.Article;
            Quantity = source.Quantity;
            TotalWorker = source.TotalWorker;
            WorkingTime = source.WorkingTime;
            TotalWorkingHours = source.TotalWorkingHours;
            TargetIE = source.TargetIE;
            TargetOfPC = source.TargetOfPC;
            OperatorAdjust = source.OperatorAdjust;
            IEPPH = source.IEPPH;
            TypeName = source.TypeName;
            OutputRateValue = source.OutputRateValue;
            PPHRateValue = source.PPHRateValue;
            ActualPPH = source.ActualPPH;
            IsMerged = source.IsMerged;
            MergeGroupID = source.MergeGroupID;
            IsVisible = source.IsVisible;
            LargestOutput = source.LargestOutput;
            PPHFallsBelowReasons = source.PPHFallsBelowReasons;
            IsSlides = source.IsSlides;
        }
    }
}
