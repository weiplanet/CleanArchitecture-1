﻿using BlazorHero.CleanArchitecture.Application.Requests.Identity;
using BlazorHero.CleanArchitecture.Client.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace BlazorHero.CleanArchitecture.Client.Pages.Identity
{
    public partial class RoleModal
    {
        private bool success;
        private string[] errors = { };
        private MudForm form;

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        [Required]
        public string Name { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        public void Cancel()
        {
            MudDialog.Cancel();
        }
        [CascadingParameter] public HubConnection hubConnection { get; set; }
        protected override async Task OnInitializedAsync()
        {
            hubConnection = hubConnection.TryInitialize(_navigationManager);
            if (hubConnection.State == HubConnectionState.Disconnected)
            {
                await hubConnection.StartAsync();
            }
        }
        private async Task SaveAsync()
        {
            form.Validate();
            if (form.IsValid)
            {
                var roleRequest = new RoleRequest() { Name = Name, Id = Id };
                var response = await _roleManager.SaveAsync(roleRequest);
                if (response.Succeeded)
                {
                    _snackBar.Add(localizer[response.Messages[0]], Severity.Success);
                    await hubConnection.SendAsync("UpdateDashboardAsync");
                    MudDialog.Close();
                }
                else
                {
                    foreach (var message in response.Messages)
                    {
                        _snackBar.Add(localizer[message], Severity.Error);
                    }
                }
            }
        }
    }
}