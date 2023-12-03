using Newtonsoft.Json.Serialization;
using PuppeteerSharp;
using TextCopy;

namespace Ulearner2;

class Program
{
    static readonly string dirToSave = @"/home/matthew/source_cs/ulearner2/konsp/";
    //  ^ Путь к директории в которой будут лежать конспекты, в конце добавь "/"
    static readonly string pathToLinks = @"/home/matthew/source_cs/ulearner2/links.txt";
    //    ^ Путь к файлу с ссылками 
    static readonly string loggerFile = @"/home/matthew/source_cs/ulearner2/log.txt";
    static List<bool> idxr;
    static async Task DownloadLastBrowserVersion()
    {
        BrowserFetcher browserFetcher = new() { Browser = SupportedBrowser.Chrome, Platform = Platform.Linux };
        await browserFetcher.DownloadAsync();
    }
    static async Task Main()
    {
        // DownloadLastBrowserVersion().Wait(); // <--- Сначала скачивает браузер, хром, лежать будет в /bin
        await File.WriteAllTextAsync(loggerFile, "");
        List<string> listOfLinks =
            (await File.ReadAllLinesAsync(pathToLinks))
            .Where(x => x.Contains("basicprogramming2")) // <-- Скачивает только второй курс, убрать - будет и первый качать
            .ToList();
        idxr = new(listOfLinks.Count);

        LaunchOptions lo = new()
        {
            Browser = SupportedBrowser.Chrome,
            Headless = false,// <----------------------------------------------- С окном или в фоне - false без, тру с окном
            DefaultViewport = new() { Height = 600, Width = 700 },
            Timeout = 15000
        };
        using var browser = await Puppeteer.LaunchAsync(lo);
        using var page = await browser.NewPageAsync();

        foreach (var item in listOfLinks)
        {
            (string? fullTitle, string? description) = await GetDescription(page, item);

            if (fullTitle is null)
                continue;
            description ??= "";
            string[] tmp = fullTitle.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string numberOfVideo = tmp[0];
            string title = string.Join(" ", tmp).Replace(numberOfVideo, "");
            string inFileContent = $"Видео {numberOfVideo}, Название: {title} \n{description}";


            string fullFileName = dirToSave + (description == "" ? "_" : "") + numberOfVideo + ".txt";
            StreamWriter sw = File.CreateText(fullFileName);
            await sw.WriteAsync(inFileContent);

            sw.Close();
            await sw.DisposeAsync();
            string log;
            if (description == "")
            {
                log = @$"{fullFileName} loaded with empty description, url: {page.Url}" + '\n';
            }
            else
            {
                log = @$"{fullFileName} loaded with inner content length: {description.Length}" + '\n';
            }
            await File.AppendAllTextAsync(loggerFile, log);
            System.Console.WriteLine(log);
        }

    }
    static async Task<(string?, string?)> GetDescription(IPage page, string url)
    {
        await page.GoToAsync(url);
        string title = await GetRightTitle(page);
        if (title is null)
            return (null, null);
        try
        {
            await page.WaitForSelectorAsync("div.bGy5H");
            await page.ClickAsync("div.bGy5H");
        }
        catch
        {
            string log = @$"Can't find text on page: {url}";
            System.Console.WriteLine(log); // <-- Отсюда срет если не нашел кнопку на странице
            await File.AppendAllTextAsync(loggerFile, log);
            return (title, null);
        }

        string exp = @"() => document.getElementsByClassName('div.bGy5H')[0]['innerHTML']";
        await page.EvaluateExpressionAsync(exp);


        return (title, await ClipboardService.GetTextAsync());
    }
    static WaitForSelectorOptions wfs = new() { Timeout = 5000 };
    private static async Task<string> GetRightTitle(IPage page)
    {
        string exp = @"document.getElementsByClassName('C8aHS Ww5W2 F3sMs')[0]['title']";
        Thread.Sleep(5000);
        string? res;
        try
        {
            await page.WaitForSelectorAsync("iframe.C8aHS.Ww5W2.F3sMs", wfs);
            res = await page.EvaluateExpressionAsync<string>(exp);
        }
        catch
        {
            string log = @$"Can't find title on page: {page.Url}";
            System.Console.WriteLine(log);
            await File.AppendAllTextAsync(loggerFile, log);
            res = null;
        }
        return res;
    }

}