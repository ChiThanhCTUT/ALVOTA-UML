using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Application.Common.Models;
using Library.Application.Features.Catalog.Commands;
using Library.Application.Features.Catalog.DTOs;
using Library.Application.Features.Catalog.Queries;
using Library.Desktop.Services;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;
    private readonly IAudioService _audio;

    [ObservableProperty] private string _searchTerm = string.Empty;
    [ObservableProperty] private int _pageIndex = 1;
    [ObservableProperty] private int _pageSize = 10;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private LookupItemDto? _selectedCategoryFilter;

    // Selected book for details
    [ObservableProperty] private BookDto? _selectedBook;

    // New Book Form Properties
    [ObservableProperty] private bool _isAddingBook;
    [ObservableProperty] private string _newTitle = string.Empty;
    [ObservableProperty] private string? _newIsbn;
    [ObservableProperty] private int? _newYear = DateTime.Now.Year;
    [ObservableProperty] private decimal _newPrice = 50000m;
    [ObservableProperty] private int _newInitialCopies = 2;
    [ObservableProperty] private string _newShelf = "Kệ A1";
    [ObservableProperty] private LookupItemDto? _newSelectedCategory;
    [ObservableProperty] private LookupItemDto? _newSelectedPublisher;
    [ObservableProperty] private LookupItemDto? _newSelectedAuthor;

    public ObservableCollection<BookDto> Books { get; } = new();
    public ObservableCollection<LookupItemDto> Categories { get; } = new();
    public ObservableCollection<LookupItemDto> Authors { get; } = new();
    public ObservableCollection<LookupItemDto> Publishers { get; } = new();

    public CatalogViewModel(IMediator mediator, IDialogService dialog, IAudioService audio)
    {
        _mediator = mediator;
        _dialog = dialog;
        _audio = audio;
    }

    [RelayCommand]
    public async Task LoadLookupsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(new GetCatalogLookupsQuery(), ct);
            if (result.IsSuccess)
            {
                Categories.Clear();
                foreach (var c in result.Value.Categories) Categories.Add(c);

                Authors.Clear();
                foreach (var a in result.Value.Authors) Authors.Add(a);

                Publishers.Clear();
                foreach (var p in result.Value.Publishers) Publishers.Add(p);

                if (Categories.Count > 0) NewSelectedCategory = Categories[0];
                if (Authors.Count > 0) NewSelectedAuthor = Authors[0];
                if (Publishers.Count > 0) NewSelectedPublisher = Publishers[0];
            }
        }
        catch
        {
            // Ignore lookup load error
        }
    }

    [RelayCommand]
    public async Task LoadBooksAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var pagedRequest = new PagedRequest(PageIndex, PageSize, SearchTerm);
            var result = await _mediator.Send(new GetBooksQuery(pagedRequest, SelectedCategoryFilter?.Id), ct);
            if (result.IsSuccess)
            {
                Books.Clear();
                foreach (var b in result.Value.Items)
                    Books.Add(b);

                TotalCount = result.Value.TotalCount;
                TotalPages = result.Value.TotalPages == 0 ? 1 : result.Value.TotalPages;
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tải danh mục sách: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchAsync(CancellationToken ct = default)
    {
        PageIndex = 1;
        await LoadBooksAsync(ct);
    }

    [RelayCommand]
    public async Task NextPageAsync(CancellationToken ct = default)
    {
        if (PageIndex < TotalPages)
        {
            PageIndex++;
            await LoadBooksAsync(ct);
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync(CancellationToken ct = default)
    {
        if (PageIndex > 1)
        {
            PageIndex--;
            await LoadBooksAsync(ct);
        }
    }

    [RelayCommand]
    public void ToggleAddBook()
    {
        IsAddingBook = !IsAddingBook;
    }

    [RelayCommand]
    public async Task SaveNewBookAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(NewTitle))
        {
            await _dialog.ShowWarningAsync("Vui lòng nhập tên đầu sách.");
            return;
        }

        if (NewSelectedCategory == null || NewSelectedPublisher == null)
        {
            await _dialog.ShowWarningAsync("Vui lòng chọn thể loại và nhà xuất bản.");
            return;
        }

        IsBusy = true;
        try
        {
            var authorIds = NewSelectedAuthor != null ? new[] { NewSelectedAuthor.Id } : Array.Empty<Guid>();
            var command = new CreateBookCommand(
                NewTitle.Trim(),
                null,
                NewIsbn,
                NewYear,
                NewSelectedCategory.Id,
                NewSelectedPublisher.Id,
                "Tiếng Việt",
                100,
                NewPrice,
                null,
                null,
                false,
                authorIds,
                NewInitialCopies,
                NewShelf);

            var result = await _mediator.Send(command, ct);
            if (result.IsFailure)
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
                _audio.PlayError();
                return;
            }

            _audio.PlaySuccess();
            await _dialog.ShowInfoAsync($"Thêm đầu sách '{NewTitle}' thành công với {NewInitialCopies} bản sao!");

            IsAddingBook = false;
            NewTitle = string.Empty;
            NewIsbn = string.Empty;

            IsBusy = false;
            await LoadBooksAsync(ct);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi lưu đầu sách: " + ex.Message);
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task AddCopiesAsync(BookDto? book, CancellationToken ct = default)
    {
        if (book == null) return;

        IsBusy = true;
        try
        {
            var command = new AddBookCopiesCommand(book.Id, 2, "Kệ A1", "CP");
            var result = await _mediator.Send(command, ct);
            if (result.IsSuccess)
            {
                _audio.PlaySuccess();
                await _dialog.ShowInfoAsync($"Đã bổ sung 2 bản sao mới cho sách '{book.Title}' thành công!");
                IsBusy = false;
                await LoadBooksAsync(ct);
            }
            else
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi thêm bản sao: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
