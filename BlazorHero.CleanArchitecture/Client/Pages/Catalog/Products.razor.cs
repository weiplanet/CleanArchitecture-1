﻿using BlazorHero.CleanArchitecture.Application.Features.Products.Queries.GetAllPaged;
using BlazorHero.CleanArchitecture.Application.Requests.Catalog;
using BlazorHero.CleanArchitecture.Client.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorHero.CleanArchitecture.Client.Pages.Catalog
{
    public partial class Products 
    {
        private IEnumerable<GetAllPagedProductsResponse> pagedData;
        private MudTable<GetAllPagedProductsResponse> table;

        private int totalItems;
        private int currentPage;
        private string searchString = null;
        [CascadingParameter] public HubConnection hubConnection { get; set; }
        protected override async Task OnInitializedAsync()
        {
            hubConnection = hubConnection.TryInitialize(_navigationManager);
            if (hubConnection.State == HubConnectionState.Disconnected)
            {
                await hubConnection.StartAsync();
            }
        }
        private async Task<TableData<GetAllPagedProductsResponse>> ServerReload(TableState state)
        {
            await LoadData(state.Page, state.PageSize);
            return new TableData<GetAllPagedProductsResponse>() { TotalItems = totalItems, Items = pagedData };
        }
        
        private ClaimsPrincipal AuthenticationStateProviderUser { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            AuthenticationStateProviderUser = await _stateProvider.GetAuthenticationStateProviderUserAsync();
        }

        private async Task LoadData(int pageNumber, int pageSize)
        {
            var request = new GetAllPagedProductsRequest { PageSize = pageSize, PageNumber = pageNumber + 1 };
            var response = await _productManager.GetProductsAsync(request);
            if (response.Succeeded)
            {
                totalItems = response.TotalCount;
                currentPage = response.CurrentPage;
                var data = response.Data;
                data = data.Where(element =>
                {
                    if (string.IsNullOrWhiteSpace(searchString))
                        return true;
                    if (element.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (element.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (element.Barcode.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        return true;
                    return false;
                }).ToList();
                pagedData = data;
            }
            else
            {
                foreach (var message in response.Messages)
                {
                    _snackBar.Add(localizer[message], Severity.Error);
                }
            }
        }

        private void OnSearch(string text)
        {
            searchString = text;
            table.ReloadServerData();
        }

        private async Task ExportToExcel()
        {
            var base64 = await _productManager.ExportToExcelAsync();
            await _jsRuntime.InvokeVoidAsync("Download", new
            {
                ByteArray = base64,
                FileName = $"products_{DateTime.Now:ddMMyyyyHHmmss}.xlsx",
                MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            });
        }

        private async Task InvokeModal(int id = 0)
        {
            var parameters = new DialogParameters();
            if (id != 0)
            {
                var product = pagedData.FirstOrDefault(c => c.Id == id);
                parameters.Add("Id", product.Id);
                parameters.Add("Name", product.Name);
                parameters.Add("Description", product.Description);
                parameters.Add("Rate", product.Rate);
                parameters.Add("Brand", product.Brand);
                parameters.Add("BrandId", product.BrandId);
                parameters.Add("Barcode", product.Barcode);
            }
            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true, DisableBackdropClick = true };
            var dialog = _dialogService.Show<AddEditProductModal>("Modal", parameters, options);
            var result = await dialog.Result;
            if (!result.Cancelled)
            {
                OnSearch("");
            }
        }

        private async Task Delete(int id)
        {
            string deleteContent = localizer["Delete Content"];
            var parameters = new DialogParameters();
            parameters.Add("ContentText", string.Format(deleteContent, id));
            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, DisableBackdropClick = true };
            var dialog = _dialogService.Show<Shared.Dialogs.DeleteConfirmation>("Delete", parameters, options);
            var result = await dialog.Result;
            if (!result.Cancelled)
            {
                var response = await _productManager.DeleteAsync(id);
                if (response.Succeeded)
                {
                    OnSearch("");
                    await hubConnection.SendAsync("UpdateDashboardAsync");
                    _snackBar.Add(localizer[response.Messages[0]], Severity.Success);
                }
                else
                {
                    OnSearch("");
                    foreach (var message in response.Messages)
                    {
                        _snackBar.Add(localizer[message], Severity.Error);
                    }
                }
            }
        }
    }
}