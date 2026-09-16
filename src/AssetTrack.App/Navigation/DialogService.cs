using AssetTrack.App.ViewModels;
using AssetTrack.App.Views;
using AssetTrack.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace AssetTrack.App.Navigation;

public class DialogService(IServiceProvider serviceProvider) : IDialogService
{
    public bool? ShowAllocateAsset(int? preselectedUserId = null)
    {
        var vm = serviceProvider.GetRequiredService<AllocateAssetDialogViewModel>();
        var window = new AllocateAssetDialog(vm, preselectedUserId) { Owner = System.Windows.Application.Current.MainWindow };
        return window.ShowDialog();
    }

    public bool? ShowReturnAsset(int? preselectedAssetId = null)
    {
        var vm = serviceProvider.GetRequiredService<ReturnAssetDialogViewModel>();
        var window = new ReturnAssetDialog(vm, preselectedAssetId) { Owner = System.Windows.Application.Current.MainWindow };
        return window.ShowDialog();
    }

    public bool? ShowAddAsset()
    {
        var vm = serviceProvider.GetRequiredService<AddAssetDialogViewModel>();
        var window = new AddAssetDialog(vm) { Owner = System.Windows.Application.Current.MainWindow };
        return window.ShowDialog();
    }

    public bool? ShowAddUser()
    {
        var vm = serviceProvider.GetRequiredService<AddUserDialogViewModel>();
        var window = new AddUserDialog(vm) { Owner = System.Windows.Application.Current.MainWindow };
        return window.ShowDialog();
    }

    public bool? ShowEditAsset(Asset asset)
    {
        var vm = serviceProvider.GetRequiredService<EditAssetDialogViewModel>();
        vm.Load(asset.Id, asset.Name, asset.Category, asset.SerialNumber, asset.Company, asset.Make, asset.Model, asset.Year);
        var window = new EditAssetDialog(vm) { Owner = System.Windows.Application.Current.MainWindow };
        return window.ShowDialog();
    }

    public bool? ShowEditUser(User user)
    {
        var vm = serviceProvider.GetRequiredService<EditUserDialogViewModel>();
        vm.Load(user.Id, user.Username, user.DisplayName, user.Role, user.Designation, user.Division, user.Company, user.City, user.Contact);
        var window = new EditUserDialog(vm) { Owner = System.Windows.Application.Current.MainWindow };
        return window.ShowDialog();
    }

    public bool? ShowMarkScrap(int assetId, string assetName, string? holderName)
    {
        var vm = serviceProvider.GetRequiredService<MarkScrapDialogViewModel>();
        vm.Load(assetId, assetName, holderName);
        var window = new MarkScrapDialog(vm) { Owner = System.Windows.Application.Current.MainWindow };
        return window.ShowDialog();
    }
}
