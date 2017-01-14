﻿using System;
using System.Collections.Generic;
using Fiddler;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Proxy = OpenQA.Selenium.Proxy;
using System.Linq;
using System.Threading;
using OpenQA.Selenium.Internal;

namespace StreamHunter
{
    public class Stream
    {
        public string StreamName { get; set; }
        public string StreamWebsite { get; set; }
        public string StreamSource { get; set; }
        public string StreamUniqueKeyword { get; set; }
        public int StreamRefreshInterval { get; set; }
        public DateTime? StreamLastRefresh { get; set; }
    }

    public class Hunter
    {
        // - Member variables
        private Proxy proxy;

        private IWebDriver driver;

        // - Path to stream file
        private readonly string streamFile;

        // - List of all streams
        private List<Stream> streams = new List<Stream>();

        // - Used to determine if proxy found stream
        private string lastFoundStream;

        // - Used to determine if stream link is valid
        private string currentStreamKeyword;

        public Hunter(string streamFile)
        {
            this.streamFile = streamFile;

            initializeStreamsFromFile();
            initializeProxy();
            initializeBrowser();
        }

        ~Hunter()
        {
            Console.WriteLine("Application exits.");
            driver.Quit();
            FiddlerApplication.Shutdown();
        }


        private void initializeProxy()
        {
            Console.WriteLine("Initializing proxy");
            FiddlerApplication.Startup(5000, true, false);
            FiddlerApplication.BeforeResponse += RequestCallback;

            // - Selenium Proxy
            proxy = new Proxy
            {
                Kind = ProxyKind.Manual,
                IsAutoDetect = false,
                HttpProxy = "localhost:5000"
            };
        }

        private void initializeBrowser()
        {
            Console.WriteLine("Initializing Browser");

            // - Create options for chrome
            var options = new ChromeOptions {Proxy = proxy};
            options.AddArgument("ignore-certificate-errors");
            options.AddArgument("mute-audio");

            // - Initialize selenium for browser automation
            driver = new ChromeDriver(options);
        }

        private void initializeStreamsFromFile()
        {
            Console.WriteLine("Reading file: " + streamFile);

            // - Read json file and parse streams
            var json = System.IO.File.ReadAllText(streamFile);
            streams = JsonConvert.DeserializeObject<List<Stream>>(json);
        }

        private void UpdateStreamsFile()
        {
            Console.WriteLine("Writing to file: " + streamFile);

            var json = JsonConvert.SerializeObject(streams);
            System.IO.File.WriteAllText(streamFile, json);
        }

        public void Schedule()
        {
            int timer = streams.OrderByDescending(x => x.StreamRefreshInterval).LastOrDefault().StreamRefreshInterval;

            foreach (Stream stream in streams)
            {
                Console.WriteLine("Navigating to: " + stream.StreamName);
                stream.StreamSource = FindStreamSource(stream.StreamWebsite, stream.StreamUniqueKeyword);
                Console.WriteLine("Found stream: " + stream.StreamSource);
            }

            Thread.Sleep(TimeSpan.FromHours(timer).Milliseconds);
        }


        public string FindStreamSource(string streamWebsite, string streamKeyword)
        {
            // - Reset lastFoundStream so we can use this to see if callback found a stream
            lastFoundStream = "";

            // - Used to find the stream
            currentStreamKeyword = streamKeyword;

            // - Navigate
            driver.Navigate().GoToUrl(streamWebsite);

            // - Wait until callback has found the stream
            while (string.IsNullOrWhiteSpace(lastFoundStream))
            {
            } // - TODO : Timeout
            return lastFoundStream;
        }

        private void RequestCallback(Session s)
        {
            // - Skip if request does not contain stream keyword
            if (!s.fullUrl.Contains(currentStreamKeyword)) return;
            lastFoundStream = s.fullUrl;
        }
    }
}