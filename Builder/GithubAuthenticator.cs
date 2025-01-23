using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls.ApplicationLifetimes;
using Octokit;
using Application = Avalonia.Application;
using System.IO;
using GuitarConfigurator.NetCore;

namespace SantrollerConfiguratorBuilder.NetCore;

using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

public class GithubAuthenticator
{
    private static readonly HttpClient Client = new();
    private const string ClientId = "Iv1.075e9e3aa23087d1";
    private const string ClientSecret = "1ecbce625e337c0ae86dc20945eed4180c023457";
    private const string RedirectUri = "http://127.0.0.1:7890/";

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {CreateNoWindow = true});
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }

    public static async Task SignIn()
    {
        // create a redirect URI using an available port on the loopback address.
        Console.WriteLine("redirect URI: " + RedirectUri);

        // create an HttpListener to listen for requests on that redirect URI.
        var http = new HttpListener();
        http.Prefixes.Add(RedirectUri);
        Console.WriteLine("Listening..");
        http.Start();

        // open system browser to start authentication
        OpenBrowser($"https://github.com/login/oauth/authorize?client_id={ClientId}");

        // wait for the authorization response.
        var context = await http.GetContextAsync();

        // sends an HTTP response to the browser.
        var response = context.Response;
        const string responseString = "<html><body>Please return to the app.</body><script</html>";
        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        await using var responseOutput = response.OutputStream;
        await responseOutput.WriteAsync(buffer);
        http.Stop();
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Activate();
        var code = HttpUtility.ParseQueryString(context.Request.Url!.Query).Get("code")!;
        var args = new Dictionary<string, string>
        {
            {"client_id", ClientId},
            {"client_secret", ClientSecret},
            {"code", code}
        };
        var url = new Uri("https://github.com/login/oauth/access_token");
        var content = new FormUrlEncodedContent(args);
        var resp = await Client.PostAsync(url, content);
        var respString = await resp.Content.ReadAsStringAsync();
        var file = Path.Join(AssetUtils.GetAppDataFolder(), "auth");
        await File.WriteAllTextAsync(file, respString);
        var accessToken = HttpUtility.ParseQueryString(respString).Get("access_token")!;
        var client2 = new GitHubClient(new ProductHeaderValue("santroller-builder"));
        var token = new Credentials(accessToken);
        client2.Credentials = token;
    }

    public static async Task<bool> RefreshToken()
    {
        var file = Path.Join(AssetUtils.GetAppDataFolder(), "auth");
        if (!Path.Exists(file))
        {
            return false;
        }

        var tokens = await File.ReadAllTextAsync(file);
        var refreshToken = HttpUtility.ParseQueryString(tokens).Get("refresh_token")!;
        var args = new Dictionary<string, string>
        {
            {"client_id", ClientId},
            {"client_secret", ClientSecret},
            {"grant_type", "refresh_token"},
            {"refresh_token", refreshToken}
        };
        var url = new Uri("https://github.com/login/oauth/access_token");
        var content = new FormUrlEncodedContent(args);
        var resp = await Client.PostAsync(url, content);
        var respString = await resp.Content.ReadAsStringAsync();
        await File.WriteAllTextAsync(file, respString);
        var accessToken = HttpUtility.ParseQueryString(respString).Get("access_token")!;
        var client2 = new GitHubClient(new ProductHeaderValue("santroller-builder"));
        var token = new Credentials(accessToken);
        client2.Credentials = token;
        try
        {
            await client2.Repository.Get("Santroller", "SantrollerConfiguratorBinaries");
        }
        catch (NotFoundException)
        {
            return false;
        }

        return true;
    }

    public static async Task<bool> CheckAccess()
    {
        var file = Path.Join(AssetUtils.GetAppDataFolder(), "auth");
        if (!Path.Exists(file))
        {
            return false;
        }

        var tokens = await File.ReadAllTextAsync(file);
        var accessToken = HttpUtility.ParseQueryString(tokens).Get("access_token")!;
        var client2 = new GitHubClient(new ProductHeaderValue("santroller-builder"));
        var token = new Credentials(accessToken);
        client2.Credentials = token;
        try
        {
            await client2.Repository.Get("Santroller", "SantrollerConfiguratorBinaries");
        }
        catch (NotFoundException)
        {
            return false;
        }

        return true;
    }

    public static void SignOut()
    {
        var file = Path.Join(AssetUtils.GetAppDataFolder(), "auth");
        File.Delete(file);
    }
}