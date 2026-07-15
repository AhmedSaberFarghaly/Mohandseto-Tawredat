using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.AdminIntegrations;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminIntegrationTests:IDisposable
{
    private sealed class PlatformTenant:ITenantProvider{public Guid? TenantId=>null;}
    private readonly string _root=Path.Combine(Path.GetTempPath(),$"mohandseto-integrations-{Guid.NewGuid():N}");
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminIntegrationService _service;
    private readonly Guid _actor=Guid.NewGuid();
    public AdminIntegrationTests(){Directory.CreateDirectory(_root);_connection=new SqliteConnection("DataSource=:memory:");_connection.Open();_db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options,new PlatformTenant());_db.Database.EnsureCreated();_service=new AdminIntegrationService(_db,DataProtectionProvider.Create(Path.Combine(_root,"keys")));}

    [Fact]public async Task Dashboard_exposes_the_eleven_design_integrations()
    {
        var result=await _service.DashboardAsync();
        Assert.Equal(11,result.Summary.Total);Assert.Equal(0,result.Summary.Connected);
        Assert.Contains(result.Integrations,x=>x.Code=="whatsapp"&&x.Provider=="Twilio");
        Assert.Contains(result.Integrations,x=>x.Code=="cloud-storage"&&!x.IsConnected);
    }

    [Fact]public async Task Configuration_is_encrypted_masked_audited_and_activated_after_test()
    {
        await SaveWhatsApp();
        var stored=await _db.IntegrationConnections.SingleAsync();
        Assert.DoesNotContain("super-secret-token",stored.ProtectedConfigJson);
        var detail=await _service.DetailAsync("whatsapp");Assert.Equal("••••••••",detail.Values["authToken"]);Assert.False(detail.Integration.IsConnected);
        var tested=await _service.TestAsync(_actor,"127.0.0.1","whatsapp");
        Assert.True(tested.Succeeded);Assert.True((await _service.DetailAsync("whatsapp")).Integration.IsConnected);
        Assert.Equal(2,await _db.IntegrationOperationLogs.CountAsync());Assert.Equal(2,await _db.AuditLogs.CountAsync());
    }

    [Fact]public async Task Connected_integration_runs_and_appears_in_filtered_log()
    {
        await SaveWhatsApp();await _service.TestAsync(_actor,null,"whatsapp");
        var result=await _service.RunAsync(_actor,null,"whatsapp",new("إرسال قالب اختبار","WA-100"));
        Assert.True(result.Succeeded);Assert.Equal("WA-100",result.Operation.Reference);
        var page=await _service.OperationsAsync("قالب","whatsapp","Succeeded",null,null,1,10);
        Assert.Equal(1,page.Total);Assert.Equal(result.Operation.Id,Assert.Single(page.Items).Id);
    }

    [Fact]public async Task Failed_operation_can_be_retried_after_connection_is_fixed()
    {
        var failed=await _service.RunAsync(_actor,null,"erp",new("مزامنة مخزون","STOCK-1"));
        Assert.False(failed.Succeeded);Assert.True(failed.Operation.IsRetryable);
        await SaveErp();await _service.TestAsync(_actor,null,"erp");
        var retry=await _service.RetryAsync(_actor,null,failed.Operation.Id);
        Assert.True(retry.Succeeded);Assert.Equal(2,retry.Operation.Attempt);Assert.NotNull(retry.Operation.ResolvedAt);
    }

    [Fact]public async Task Disable_blocks_new_runs_and_bulk_retry_respects_connection_state()
    {
        await SaveWhatsApp();await _service.TestAsync(_actor,null,"whatsapp");await _service.DisableAsync(_actor,null,"whatsapp");
        var failed=await _service.RunAsync(_actor,null,"whatsapp",new());Assert.False(failed.Succeeded);
        Assert.Equal(0,await _service.RetryAllAsync(_actor,null));
        var page=await _service.OperationsAsync(null,null,"Failed",null,null,1,25);Assert.Single(page.Items);Assert.Equal(2,page.Items[0].Attempt);
    }

    private Task<IntegrationDetailDto> SaveWhatsApp()=>_service.SaveAsync(_actor,null,"whatsapp",new(new Dictionary<string,string>{{"provider","Twilio"},{"businessPhone","01000001678"},{"accountSid","AC123"},{"authToken","super-secret-token"},{"verificationStatus","موثق"},{"approvedTemplates","12"}}));
    private Task<IntegrationDetailDto> SaveErp()=>_service.SaveAsync(_actor,null,"erp",new(new Dictionary<string,string>{{"provider","SAP"},{"endpoint","https://sap.example.com/api"},{"companyCode","EG01"},{"username","sync-user"},{"password","erp-secret"},{"ordersInterval","15 minutes"},{"inventoryInterval","Hourly"}}));
    public void Dispose(){_db.Dispose();_connection.Dispose();if(Directory.Exists(_root))Directory.Delete(_root,true);}
}
