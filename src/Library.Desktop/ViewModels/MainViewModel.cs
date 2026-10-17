using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Desktop.Services;
using Library.Infrastructure.Services;

namespace Library.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly CurrentUserService _currentUser;
    private readonly IDialogService _dialog;

    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private string _currentUserName = "Quản trị viên";
    [ObservableProperty] private string _currentUserRole = "Admin";
    [ObservableProperty] private string _schoolName = "THƯ VIỆN TRƯỜNG HỌC";
    [ObservableProperty] private string _selectedMenu = "Dashboard";

    public bool CanAccessSystem => CurrentUserRole == "Admin";
    public bool CanAccessCatalogAndFines => CurrentUserRole != "Assistant";

    // ViewModels
    public DashboardViewModel DashboardVM { get; }
    public CirculationViewModel CirculationVM { get; }
    public CatalogViewModel CatalogVM { get; }
    public MemberViewModel MemberVM { get; }
    public FineViewModel FineVM { get; }
    public InventoryViewModel InventoryVM { get; }
    public ReportViewModel ReportVM { get; }
    public SystemViewModel SystemVM { get; }

    public MainViewModel(
        INavigationService navigation,
        CurrentUserService currentUser,
        IDialogService dialog,
        DashboardViewModel dashboardVM,
        CirculationViewModel circulationVM,
        CatalogViewModel catalogVM,
        MemberViewModel memberVM,
        FineViewModel fineVM,
        InventoryViewModel inventoryVM,
        ReportViewModel reportVM,
        SystemViewModel systemVM)
    {
        _navigation = navigation;
        _currentUser = currentUser;
        _dialog = dialog;

        DashboardVM = dashboardVM;
        CirculationVM = circulationVM;
        CatalogVM = catalogVM;
        MemberVM = memberVM;
        FineVM = fineVM;
        InventoryVM = inventoryVM;
        ReportVM = reportVM;
        SystemVM = systemVM;

        CurrentUserName = _currentUser.Username ?? "Quản trị viên";
        CurrentUserRole = _currentUser.Role ?? "Admin";

        // Mặc định hiển thị Dashboard
        CurrentView = DashboardVM;
        _ = DashboardVM.LoadDataAsync();
    }

    [RelayCommand]
    public void Navigate(string viewName)
    {
        SelectedMenu = viewName;
        switch (viewName)
        {
            case "Dashboard":
                CurrentView = DashboardVM;
                _ = DashboardVM.LoadDataAsync();
                break;
            case "Circulation":
                CurrentView = CirculationVM;
                _ = CirculationVM.LoadOverdueLoansAsync();
                break;
            case "Catalog":
                CurrentView = CatalogVM;
                _ = CatalogVM.LoadLookupsAsync();
                _ = CatalogVM.LoadBooksAsync();
                break;
            case "Members":
                CurrentView = MemberVM;
                _ = MemberVM.LoadMembersAsync();
                break;
            case "Fines":
                CurrentView = FineVM;
                _ = FineVM.LoadFinesAsync();
                break;
            case "Inventory":
                CurrentView = InventoryVM;
                _ = InventoryVM.LoadSessionsAsync();
                break;
            case "Reports":
                CurrentView = ReportVM;
                _ = ReportVM.GenerateReportAsync();
                break;
            case "System":
                CurrentView = SystemVM;
                _ = SystemVM.LoadSettingsAsync();
                _ = SystemVM.LoadUsersAsync();
                _ = SystemVM.LoadAuditLogsAsync();
                break;
        }
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        var confirm = await _dialog.ShowConfirmationAsync("Bạn có chắc chắn muốn đăng xuất?");
        if (!confirm) return;

        _currentUser.Clear();
        _navigation.NavigateTo<LoginViewModel>();
    }
}
