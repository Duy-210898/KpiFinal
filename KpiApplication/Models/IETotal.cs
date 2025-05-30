using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace KpiApplication.Models
{
    public class IETotal : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
#if DEBUG
            try
            {
                var value = GetType().GetProperty(propertyName)?.GetValue(this);
            }
            catch (Exception ex)
            {
            }
#endif
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }



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

        private bool _outsourcingAssemblingBool;
        public bool OutsourcingAssemblingBool { get => _outsourcingAssemblingBool; set => SetProperty(ref _outsourcingAssemblingBool, value); }

        private bool _outsourcingStitchingBool;
        public bool OutsourcingStitchingBool { get => _outsourcingStitchingBool; set => SetProperty(ref _outsourcingStitchingBool, value); }

        private bool _outsourcingStockFittingBool;
        public bool OutsourcingStockFittingBool { get => _outsourcingStockFittingBool; set => SetProperty(ref _outsourcingStockFittingBool, value); }

        private string _dataStatus;
        public string Status { get => _dataStatus; set => SetProperty(ref _dataStatus, value); }

        private string _process;
        public string Process { get => _process; set => SetProperty(ref _process, value); }

        private int? _targetOutputPC;
        public int? TargetOutputPC
        {
            get => _targetOutputPC;
            set
            {
                if (SetProperty(ref _targetOutputPC, value))
                    RecalculateIEPPH();
            }
        }

        private int? _adjustOperatorNo;
        public int? AdjustOperatorNo
        {
            get => _adjustOperatorNo; 
            set
            {
                if (SetProperty(ref _adjustOperatorNo, value))
                    RecalculateIEPPH();
            }
        }

        private double? _iePPHValue;
        public double? IEPPHValue
        {
            get => _iePPHValue;
            private set => SetProperty(ref _iePPHValue, value);
        }
        private double? _tctValue;
        public double? TCTValue
        {
            get => _tctValue;
            set => SetProperty(ref _tctValue, value);
        }

        private string _typeName;
        public string TypeName { get => _typeName; set => SetProperty(ref _typeName, value); }

        private string _isSigned;
        public string IsSigned { get => _isSigned; set => SetProperty(ref _isSigned, value); }

        private string _referenceModel;
        public string ReferenceModel { get => _referenceModel; set => SetProperty(ref _referenceModel, value); }

        private int? _referenceOperator;
        public int? ReferenceOperator
        {
            get => _referenceOperator;
            set
            {
                if (SetProperty(ref _referenceOperator, value))
                    RecalculateFinalOperator();
            }
        }

        private int? _operatorAdjust;
        public int? OperatorAdjust
        {
            get => _operatorAdjust;
            set
            {
                if (SetProperty(ref _operatorAdjust, value))
                    RecalculateFinalOperator();
            }
        }

        private int? _finalOperator;
        public int? FinalOperator
        {
            get => _finalOperator;
            private set => SetProperty(ref _finalOperator, value);
        }

        private string _notes;
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

        private int _articleID; // Bảng Article
        public int ArticleID { get => _articleID; set => SetProperty(ref _articleID, value); }

        private int? _typeID; // Bảng ArtType
        public int? TypeID { get => _typeID; set => SetProperty(ref _typeID, value); }

        private int _ieID; // Bảng IE_PPH_Data
        public int IEID { get => _ieID; set => SetProperty(ref _ieID, value); }

        private int _processID; // Bảng Process
        public int ProcessID { get => _processID; set => SetProperty(ref _processID, value); }

        private int _stageID; // Bảng Production_Stages
        public int StageID { get => _stageID; set => SetProperty(ref _stageID, value); }

        private void RecalculateIEPPH()
        {
            if (TargetOutputPC.HasValue && AdjustOperatorNo.HasValue && AdjustOperatorNo.Value != 0)
            {
                IEPPHValue = Math.Round((double)TargetOutputPC.Value / AdjustOperatorNo.Value, 2);
            }
            else
            {
                IEPPHValue = null;
            }
        }
        private void RecalculateFinalOperator()
        {
            if (ReferenceOperator.HasValue)
            {
                int adjustValue = OperatorAdjust ?? 0;
                FinalOperator = ReferenceOperator.Value + adjustValue;
            }
            else
            {
                FinalOperator = null;
            }
        }
        public static IETotal Clone(IETotal original)
        {
            if (original == null) return null;

            return new IETotal
            {
                ArticleID = original.ArticleID,
                TypeID = original.TypeID,
                IEID = original.IEID,
                ProcessID = original.ProcessID,
                StageID = original.StageID,

                ArticleName = original.ArticleName,
                ModelName = original.ModelName,
                PCSend = original.PCSend,
                PersonIncharge = original.PersonIncharge,
                NoteForPC = original.NoteForPC,

                OutsourcingAssemblingBool = original.OutsourcingAssemblingBool,
                OutsourcingStitchingBool = original.OutsourcingStitchingBool,
                OutsourcingStockFittingBool = original.OutsourcingStockFittingBool,

                Status = original.Status,
                Process = original.Process,

                TargetOutputPC = original.TargetOutputPC,
                AdjustOperatorNo = original.AdjustOperatorNo,

                TypeName = original.TypeName,
                IsSigned = original.IsSigned,
                ReferenceModel = original.ReferenceModel,

                ReferenceOperator = original.ReferenceOperator,
                OperatorAdjust = original.OperatorAdjust,
                TCTValue = original.TCTValue,

                Notes = original.Notes
            };
        }
        public List<string> GetChangedProperties(IETotal original)
        { 
            var changedProperties = new List<string>();

            if (original == null) return changedProperties;

            var properties = typeof(IETotal).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // Lọc bỏ các property không cần so sánh (nếu có)
                if (!prop.CanRead) continue;

                var originalValue = prop.GetValue(original);
                var currentValue = prop.GetValue(this);

                bool changed;
                if (originalValue == null && currentValue == null)
                    changed = false;
                else if (originalValue == null || currentValue == null)
                    changed = true;
                else
                    changed = !originalValue.Equals(currentValue);

                if (changed)
                    changedProperties.Add(prop.Name);
            }

            return changedProperties;

        }
    }
}
