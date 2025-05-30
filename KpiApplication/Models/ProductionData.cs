using KpiApplication.DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace KpiApplication.Models
{
    public class ProductionData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ProductionData()
        {
            ScanDate = DateTime.Today;
            IsVisible = true;
        }

        public ProductionData Clone() => (ProductionData)this.MemberwiseClone();

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

        public Action<ProductionData, string> OnMergeStatusChanged;

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

        // Basic Production Info
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
        private int? _target;
        public int? Target
        {
            get => _target;
            set => SetProperty(ref _target, value);
        }


        private double? _iePPH;
        public double? IEPPH
        {
            get => _iePPH;
            set
            {
                if (SetProperty(ref _iePPH, value))
                    Recalculate();
            }
        }
        private string _typeName; 
        public string TypeName
        {
            get => _typeName;
            set => SetProperty(ref _typeName, value);
        }

        private double? _rate;
        public double? Rate
        {
            get => _rate;
            set
            {
                if (SetProperty(ref _rate, value))
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

        public string OutputRate => Rate.HasValue ? $"{Rate.Value * 100:#0.0}%" : string.Empty;
        public string PPHRate => PPHRateValue.HasValue ? $"{PPHRateValue.Value * 100:#0.0}%" : string.Empty;

        private bool _isMerged;
        public bool IsMerged
        {
            get => _isMerged;
            set => SetProperty(ref _isMerged, value);
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
            Target = (TotalWorker.HasValue && WorkingTime.HasValue && IEPPH.HasValue)
                ? (int?)Math.Round(TotalWorker.Value * WorkingTime.Value * IEPPH.Value, 1)
                : null;
        }
        private void CalculateActualPPH()
        {
            if (TotalWorker.HasValue && WorkingTime.HasValue && Quantity.HasValue)
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
            if (TotalWorker.HasValue && WorkingTime.HasValue)
            {
                TotalWorkingHours = Math.Round(TotalWorker.Value * WorkingTime.Value, 1);
            }
            else
            {
                TotalWorkingHours = null;
            }
        }

        private void CalculateOutputRate()
        {
            if (Target.HasValue && Target > 0 && Quantity.HasValue)
            {
                Rate = Math.Round((double)Quantity.Value / Target.Value, 4);
            }
            else
            {
                Rate = null;
            }
        }

        private void CalculatePPHRate()
        {
            if (IEPPH.HasValue && Target.HasValue && Target > 0 && ActualPPH.HasValue)
            {
                PPHRateValue = Math.Round(ActualPPH.Value / IEPPH.Value, 4);
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
    }
}
