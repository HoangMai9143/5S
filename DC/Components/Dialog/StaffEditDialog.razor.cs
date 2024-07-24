using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using DC.Models;


namespace DC.Components.Dialog
{
  public partial class StaffEditDialog
  {

    [CascadingParameter] MudDialogInstance MudDialog { get; set; }
    [Parameter] public StaffModel Staff { get; set; }

    private StaffModel staff = new StaffModel();

    protected override void OnInitialized()
    {
      if (Staff != null)
      {
        staff = new StaffModel
        {
          Id = Staff.Id,
          FullName = Staff.FullName,
          Department = Staff.Department,
          IsActive = Staff.IsActive
        };
      }
    }

    private void Submit()
    {
      MudDialog.Close(DialogResult.Ok(staff));
    }

    private void Cancel() => MudDialog.Cancel();
  }
}