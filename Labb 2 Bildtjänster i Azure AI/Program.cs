using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

// Import namespaces
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

//Caroline Uthawong-Burr  .net.23
namespace image_analysis
{
    class Program
    {

        private static ComputerVisionClient cvClient;
        static async Task Main(string[] args)
        {

            try
            {
                //0. Sätt upp alla nycklar via appsettings.json//
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string cogSvcEndpoint = configuration["CognitiveServicesEndpoint"];
                string cogSvcKey = configuration["CognitiveServiceKey"];


                //1. Be användaren om en bildadress (lokal sökväg)
                Console.WriteLine("Enter the path to your image");

                //2. Spara användarens svar i en variabel

                string imageFile = Console.ReadLine(); //imageFile kommer användas som användarens bild//


                // Authenticate Azure AI Vision client
                ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(cogSvcKey);
                cvClient = new ComputerVisionClient(credentials)
                {
                    Endpoint = cogSvcEndpoint
                };

                //3. Analysera användarens bild
                // Analyze image
                await AnalyzeImage(imageFile);


                //4. Skapa en thumbnail av användarens bild
                // Get thumbnail
                await GetThumbnail(imageFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task AnalyzeImage(string imageFile)
        {
            Console.WriteLine($"Analyzing {imageFile}");

            // Specify features to be retrieved
            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            {
               VisualFeatureTypes.Description,
               VisualFeatureTypes.Tags,
               VisualFeatureTypes.Categories,
               VisualFeatureTypes.Brands,
               VisualFeatureTypes.Objects,
               VisualFeatureTypes.Adult
            };

            // Get image analysis
            using (var imageData = File.OpenRead(imageFile))
            {
                var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);

                // Hämta bildtexter
                foreach (var caption in analysis.Description.Captions)
                {
                    Console.WriteLine($"Beskrivning: {caption.Text} (tillförlitlighet: {caption.Confidence.ToString("P")})");
                }

                // Hämta bildtaggar
                if (analysis.Tags.Count > 0)
                {
                    Console.WriteLine("Taggar:");
                    foreach (var tag in analysis.Tags)
                    {
                        Console.WriteLine($" -{tag.Name} (tillförlitlighet: {tag.Confidence.ToString("P")})");
                    }
                }
                // Hämta bildkategorier
                List<LandmarksModel> landmarks = new List<LandmarksModel>();
                Console.WriteLine("Kategorier:");
                foreach (var category in analysis.Categories)  // Ändrat 'i' till 'in'
                {
                    // Skriv ut kategorin
                    Console.WriteLine($" - {category.Name} (tillförlitlighet: {category.Score.ToString("P")})");

                    // Hämta landmärken i denna kategori
                    if (category.Detail?.Landmarks != null)
                    {
                        foreach (var landmark in category.Detail.Landmarks)  // Ändrat 'i' till 'in' och 'LandmarksModel' till 'var'
                        {
                            if (!landmarks.Any(item => item.Name == landmark.Name))
                            {
                                landmarks.Add(landmark);
                            }
                        }
                    }
                }

                // Om det finns landmärken, lista dem
                if (landmarks.Count > 0)
                {
                    Console.WriteLine("Landmärken:");
                    foreach (var landmark in landmarks)  // Ändrat 'i' till 'in' och 'LandmarksModel' till 'var'
                    {
                        Console.WriteLine($" - {landmark.Name} (tillförlitlighet: {landmark.Confidence.ToString("P")})");
                    }
                }

                // Hämta varumärken i bilden
                if (analysis.Brands.Count > 0)
                {
                    Console.WriteLine("Varumärken:");
                    foreach (var brand in analysis.Brands)  // Ändrat 'i' till 'in'
                    {
                        Console.WriteLine($" - {brand.Name} (tillförlitlighet: {brand.Confidence.ToString("P")})");
                    }
                }


                // Hämta objekt i bilden
                if (analysis.Objects.Count > 0)
                {
                    Console.WriteLine("Objekt i bilden:");



                    foreach (var detectedObject in analysis.Objects)
                    {
                        // Skriv ut objektnamn
                        Console.WriteLine($" -{detectedObject.ObjectProperty} (tillförlitlighet: {detectedObject.Confidence.ToString("P")})");
                    }
                }


                // Hämta måttlighetsbedömningar
                string ratings = $"Bedömningar:\\n -Vuxet: {analysis.Adult.IsAdultContent}\\n -Racy: {analysis.Adult.IsRacyContent}\\n -Blodigt: {analysis.Adult.IsGoryContent}";
                Console.WriteLine(ratings);

            }

        }

        static async Task GetThumbnail(string imageFile)
        {
            Console.WriteLine("Generating thumbnail");

            // Generera en miniatyrbild
            try
            {
                using (var imageData = File.OpenRead(imageFile))
                {
                    // Hämta miniatyrdata
                    var thumbnailStream = await cvClient.GenerateThumbnailInStreamAsync(100, 100, imageData, true);

                    // Spara miniatyrbilden
                    string thumbnailFileName = "thumbnail.png";
                    using (Stream thumbnailFile = File.Create(thumbnailFileName))
                    {
                        await thumbnailStream.CopyToAsync(thumbnailFile);  // Använd await här också för asynkron kopiering
                    }

                    Console.WriteLine($"Miniatyrbild sparad i {thumbnailFileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ett fel inträffade: {ex.Message}");
            }

        }


    }
}