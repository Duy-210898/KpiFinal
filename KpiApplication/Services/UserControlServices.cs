using DevExpress.XtraBars.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Services
{
    internal class UserControlServices
    {
        private readonly NavigationFrame _navigationFrame;
        private readonly Dictionary<Type, (UserControl Control, NavigationPage Page)> _cachedControls;

        public UserControlServices(NavigationFrame navigationFrame)
        {
            _navigationFrame = navigationFrame ?? throw new ArgumentNullException(nameof(navigationFrame));
            _cachedControls = new Dictionary<Type, (UserControl, NavigationPage)>();
        }

        public async Task ShowAsync(Type controlType, ILoadingService loadingService = null)
        {
            if (controlType == null)
                throw new ArgumentNullException(nameof(controlType));

            if (!typeof(UserControl).IsAssignableFrom(controlType))
                throw new ArgumentException($"{controlType.Name} is not a UserControl.");

            // Nếu control đã được tạo → hiển thị lại
            if (_cachedControls.TryGetValue(controlType, out var cached))
            {
                _navigationFrame.SelectedPage = cached.Page;
                return;
            }

            // Chưa có → tạo mới
            var control = (UserControl)Activator.CreateInstance(controlType);
            control.Dock = DockStyle.Fill;

            var page = new NavigationPage();
            page.Controls.Add(control);

            _navigationFrame.Pages.Add(page);
            _cachedControls[controlType] = (control, page);

            // Nếu UserControl hỗ trợ LoadDataAsync → tải trước khi hiển thị
            if (control is ISupportLoadAsync loadable)
            {
                if (loadingService != null)
                {
                    await loadingService.ShowLoadingAsync("Loading", $"Loading {controlType.Name}...", async () =>
                    {
                        await loadable.LoadDataAsync();
                    });
                }
                else
                {
                    await loadable.LoadDataAsync();
                }
            }

            // Chỉ hiển thị sau khi dữ liệu đã load xong
            _navigationFrame.SelectedPage = page;
        }
        public async Task ReloadAsync<T>() where T : UserControl
        {
            if (_cachedControls.TryGetValue(typeof(T), out var cached) && cached.Control is ISupportLoadAsync loadable)
            {
                await loadable.LoadDataAsync();
            }
        }

        public T Get<T>() where T : UserControl
        {
            return _cachedControls.TryGetValue(typeof(T), out var cached) ? cached.Control as T : null;
        }

        public bool IsVisible<T>() where T : UserControl
        {
            if (_cachedControls.TryGetValue(typeof(T), out var cached))
                return _navigationFrame.SelectedPage == cached.Page;

            return false;
        }

        public void RefreshIfExists<T>(Action<T> refreshAction) where T : UserControl
        {
            var ctrl = Get<T>();
            if (ctrl != null && IsVisible<T>())
                refreshAction(ctrl);
        }

        public IEnumerable<UserControl> GetAllControls()
        {
            return _cachedControls.Values.Select(v => v.Control);
        }
    }
}
