using System.Resources;
using System.Runtime.InteropServices;
using HtmlAgilityPack;
namespace Car_Collector_9000
{
    internal static class Program
    {
        //constants for changing background
        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPIF_UPDATEINFILE = 1;
        public const int SPIF_SENDCHANGE = 2;
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
           
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.ContextMenuStrip = GetContext();
            notifyIcon.Icon = new Icon("Car-Collector-9000.ico");
            notifyIcon.Visible = true;

            Application.Run();
        }


        private static ContextMenuStrip GetContext()
        {
            ContextMenuStrip context = new ContextMenuStrip();

            //get wallpaper method
            _ = context.Items.Add("Wallpaper!", null, new EventHandler(Wallpaper_Click));

            _ = context.Items.Add("Exit", null, new EventHandler(Exit_Click));
            return context;
        }

        private static void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private static void Wallpaper_Click(object sender, EventArgs e)
        {
            Task done = scraper();
        }

        static async Task scraper()
        {
            Console.WriteLine("Welcome to Image Extractor 9000!");

            var targetUrl = "";
            var pictureFolder = "";

            //select right url from theme here
            targetUrl = "http://www.speedhunters.com/";
            //check if the folder has been updated within the last 
            pictureFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\scraperImages";
            bool renew = outDated(pictureFolder);

            //if folder was renewed within 7 days
            //pick random image
            if (!renew)
            {
                //get a random article to scrape
                string page = getArticle(targetUrl);
                List<string> images;
                images = parseHtml(page);
                Boolean imagesDownloaded = await downloadImages(pictureFolder, images);
                setDesktop(pictureFolder);
            }
            else
            {
                setDesktop(pictureFolder);
            }
        }

        //checks if folder not empty
        //returns a boolean indicating if the images are not outdated
        //returns true if images were modified within last 7 days
        //returns false otherwise
        private static bool outDated(string folder)
        {
            //check if empty first
            var currentFiles = Directory.GetFiles(folder);
            Console.WriteLine(currentFiles.Length);
            if (currentFiles.Length <= 2)
            {
                Console.WriteLine("empty folder");
                return false;
            }

            DateTime curr = DateTime.Now;
            var lastModified = System.IO.File.GetLastWriteTime(folder);
            var delta = curr - lastModified;
            Console.WriteLine(delta.Days);
            if (delta.Days <= 7) { return true; }
            return false;
        }

        //method to request a url from a webpage
        private static async Task<string> requestPage(string url)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(url);
            return response;
        }

        //parsing the HTML for img tags
        private static List<string> parseHtml(string html)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var links = doc.DocumentNode.Descendants("img").
                Where(node => node.GetAttributeValue("class", "").Contains("alignnone")).ToList();

            List<string> imgLinks = new List<string>();
            foreach (var link in links)
            {
                var data = link.Attributes.Single(att => att.Name == "data-go-fullscreen").Value;
                imgLinks.Add(data);
            }
            return imgLinks;
        }

        //just your average random function
        private static int getRandom(int max)
        {
            Random random = new Random();
            return random.Next(0, max);
        }

        //find a random article to scrape
        private static string getArticle(string target)
        {
            Console.WriteLine("Getting random article");
            //we have a good URl so we make a request and store the response
            string page = requestPage(target).Result;
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(page);

            var articles = doc.DocumentNode.Descendants("a").
                Where(node => node.GetAttributeValue("class", "").Contains("content-thumbnail")).ToList();

            //selecting our random article and its link 
            int tar = getRandom(articles.Count);
            var data = articles[tar].Attributes[1].Value;

            return requestPage(data).Result;
        }

        //downloading the image from the URL
        private static async Task<Boolean> downloadImages(string filePath, List<string> links)
        {
            var HttpClient = new HttpClient();
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            else
            {
                //only get new image 
                DateTime lastModified = Directory.GetLastWriteTime(filePath);
            }

            for (var i = 0; i < links.Count; i++)
            {
                var fileName = "image" + i + ".jpg";
                var imageBytes = await HttpClient.GetByteArrayAsync(links[i]);
                var path = Path.Combine(filePath, $"{fileName}");
                await File.WriteAllBytesAsync(path, imageBytes);
            }
            return true;
        }

        //pick a random one from the available images
        //update background
        //deletes image
        public static void setDesktop(String folderPath)
        {
            Console.WriteLine("Setting Desktop Wallpaper");
            var rand = new Random();
            var files = Directory.GetFiles(folderPath);
            var imagePath = files[rand.Next(files.Length) - 1];
            Console.WriteLine(imagePath);
            //updating the desktop
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINFILE | SPIF_SENDCHANGE);
            System.IO.File.Delete(imagePath);
        }
    }
}