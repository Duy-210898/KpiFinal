using System;
using System.ComponentModel;

public class BonusDocument_Model : INotifyPropertyChanged
{
    private string _fileName;

    public int Id { get; set; }
    public string ModelName { get; set; }

    public string FileName
    {
        get => _fileName;
        set
        {
            if (_fileName != value)
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }
    }
    public string DocumentType { get; set; }
    public byte[] PdfData { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public string CreatedByName { get; set; }
    public string UpdatedByName { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
