using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using ObjektvisionTestClient.ImportServer;

namespace ObjektvisionTestClient
{
    class Program
    {
        private static int brokerId = 0;
        private static string password = "";
        private static ImportServerSoapClient server;
        

        static void Main(string[] args)
        {
            Run();
        }

        public static void Run()
        {            
            using (server = new ImportServerSoapClient())
            {              
                if (!Login())
                {
                    Log("Login failed");
                    Console.ReadLine();
                    return;
                }
                Log("Logged in as " + brokerId);

                //The list of all objects
                var listOfObjects = server.GetAdvertList().ToList();
                Log("Number of objects: " + listOfObjects.Count);

                Log();
                //The leads for this customer
                //This function returns all new leads for all objects since the last time the function was run.
                //To get a nice popup in the recieving system it might be a good idea to run this function every 10-15 minutes or so,
                //otherwise once per day. Remember to store the data locally, as it will not be sent again once it has been sent.
                var leads = server.GetLeads().ToList();
                Log("Number of leads since last get: " + leads.Count);
                foreach (var l in leads)
                {
                    Log();
                    Log("Estate: " + l.ServerID);
                    Log("Errand: " + l.Errand);
                    Log("Message: " + l.Message);
                }


                //The impressions (visitor statistics) for the user/client
                var impressions = server.GetAdvertImpressionsList(new DateTime(2000,1,1), DateTime.MaxValue).ToList();
                foreach (var l in impressions)
                {
                    Log("Estate: " + l.ServerID);
                    Log("Number of leads: " + l.Site[0].Impressions.Length);
                }

                Console.ReadLine();

                //Try and validate the estate to see what errors we get (if any)
                //Validate(GetEstate(listOfObjects[0].ServerID));
                //Log();

                //Get the estate, change a field, save it, get it again, read the field and see that it is changed, change it again and save again.
                //TestUpdate(listOfObjects[0].ServerID);

                Log();

                var estate = CreateNew();
                Validate(estate);

                var updateMessage = server.Update(estate);
                Log();
                Log("Saving estate");
                Log("Save successfully: " + updateMessage.Success);
                Log("Is new estate: " + updateMessage.NewEstate);
                Log("Server ID: " + updateMessage.ServerID);
                if (updateMessage.Success)
                    Log("Estate objectpage can be found at: http://www.tnext.test.objektvision.se/Description/" +
                        updateMessage.ServerID);
                else
                {
                    Log("Something broke when saving");
                    Log(updateMessage.Message);
                }

                Log("Press any key to delete the newly created object");
                Console.ReadLine();
                Log("Deleted successfully: " + server.DeleteByServerID(updateMessage.ServerID));
                Console.ReadLine();
            }
        }

        static void Validate(Estate estate)
        {
            var validationMessage = server.Validate(estate);
            Log();
            Log("Validating estate");
            Log("Validation success: " + validationMessage.Success);
            Log("Validation message: " + validationMessage.Message);
        }

        static void TestUpdate(int serverId)
        {
            var estate = GetEstate(serverId);

            Log("Estate: " + estate.Address.StreetAddress + ", " + estate.Address.PostalCode + " " + estate.Address.City);
            Log("Size: " + (estate.InternetDisplay[0] as Premise).TotalArea);
            Log("Client ID: " + estate.ClientID);

            var oldClientId = estate.ClientID;
            estate.ClientID = "testingtesting";
            Log("Updated: " + server.Update(estate).Message);

            var getEstateAgain = GetEstate(serverId);

            Log("Updated client Id: " + getEstateAgain.ClientID);
            Log();

            estate.ClientID = oldClientId;
            server.Update(estate);
        }

        static Estate CreateNew()
        {
            var e = new Estate();
            e.ClientID = "ObjektvisionTest";

            #region Address
            e.Address = new Address()
            {
                MunicipalityCode = 91,
                City = "Helsinki",
                PostalCode = "00180",
                StreetAddress = "Energiakatu 6",
                Coordinate = new Coordinate()
                {
                    System = CoordinateSystem.WGS84,
                    X = (decimal) 60.16613,
                    Y = (decimal) 24.903131
                },
                CountryCode = "FI"
            };
            #endregion
            
            #region Attachments
            var attach = new List<AbstractAttachment>();
            attach.Add(new AttachmentImage()
            {
                Category = ImageCategories.Exterior,
                ClientID="image1.jpg",
                Content = new AttachmentRemoteContent()
                {
                    URL = "http://1.bp.blogspot.com/-FtiNAuEFPYU/VLVzq5_gjxI/AAAAAAACqWo/2QX5ixVjfpw/s1600/Nice%2520Jan%25202015%2520416.JPG"
                },
                Description = "A nice view of the castle"
            });
            attach.Add(new AttachmentLink()
            {
                Description = "A link to something",
                URL= "https://klockren.nu/"
            });
            e.Attachments = attach.ToArray();
            #endregion

            #region Contacts
            //Use estate.Contacts instead of legacy estate.Contact. Limited to two contacts at the moment
            e.Contacts = new List<Contact>()
            {
                new Contact()
                {
                    CellPhone = "073-123 456",
                    Email = "sometestemail@sometestdomain.com",
                    Name="Some Testperson",
                    Title="CTO",
                    Phone="08-123 456",
                    Image = new ContactImage()
                    {
                        ClientID = "contactimage.jpg",
                        Content = new AttachmentBase64Content() {
                            Base64EncodedContent = "R0lGODlhEAALAMZwADQyKTY1KTg3KTo4Kzo4LD46LT08LTs+Oz8+M0I/MEBAOURBM0BCQERDOUZFOERFREdHOUtHOEdIR0xJOUxKO0lKR1FOPU9OQ1VRP1dTQFhUQllUQ1hXTVtXRV1ZR15ZRlhZWGBb" +
                                                    "SFtdV2JdSmFfUGJhVGdhT2diTmdjT2RlZGtlUWhmXWhoYW5pVnJtWHhzY3x3ZIF4Ynp6coJ+bIh+Z4CAgIWAc4aGhoqHf46Id4iIiI2KfY2LjZWRgpaSg5SVlJ2UgpeVj5uZm6Ccj56elqSdkqeeh5+g" +
                                                    "n6agkaSgmKGioKuporKplq2tqbOysLe2sbe3t7q6tsG8ssK9tMW9rsjGvsvJyczKwtDOyM/Oy9TSzdzX0t3Yz9zb2uHf2+Ph4ebi2eXk4efn5+np6erq6u3t7PLx7/Ly8fX19fb29fj28vj39vn5+Pr5" +
                                                    "+P39/P7+/v///////////////////////////////////////////////////////////////ywAAAAAEAALAAAHiIBwgmqChYaCV4VgXIeGWD2FbUZSbodvVS07hlQ0Pl5rbGlaQyoeS4ZhTEBTSDAm" +
                                                    "IR0aJGdkOjdbNU5mUFYzHxMJFFlwR0IHKzhFXxI8LxgJAk2CPwwPIi4xOUk2GxEFF2iCYiApLBknKCMWDQsGUYZjQQ4EAwEACCUQT41wXUoVCjjIIFLmUCAAOw=="
                        }
                    }
                },
                new Contact()
                {
                    CellPhone = "073-456 123",
                    Email = "sometestemail2@sometestdomain.com",
                    Name="SomeOther Testperson",
                    Title="CEO",
                    Phone="08-456 123",
                    Image = new ContactImage()
                    {
                        ClientID = "contactimage2.jpg",
                        Content = new AttachmentRemoteContent() {
                            URL = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d2/Donald_Trump_August_19,_2015_(cropped).jpg/220px-Donald_Trump_August_19,_2015_(cropped).jpg"
                        }
                    }
                }
            }.ToArray();
            #endregion

            #region Premise properties
            var display = new Premise();
            display.TotalArea = 500;
            display.AdjustablePlan = false;
            display.BuildYear = 1984;
            display.RebuildYear = 2009;
            display.Floor = 2;
            display.FloorsInBuilding = 4;
            display.Rooms = 5;
            display.Types =
                new[]
                {
                    PremiseTypes.Office,
                    PremiseTypes.OfficeHotel,
                    PremiseTypes.Shop
                };
            display.Contract = new ContractPremise()
            {
                AverageFeePerSquareMeterAndYear = new PriceWithInfo() { Amount = 500,Currency = "EUR",Info="Best prices in town"},
                ContractType = ContractPremiseType.Sale,
                //ContractType = ContractPremiseType.Rent,
                //ContractType = ContractPremiseType.SaleAndRent,
                PossessionDate = new DateAndTime {Value=DateTime.Now.AddMonths(1)},
                PossessionText = "Access within a month",

                //Price is only applicable if the type is Sale or SaleAndRent
                Price = new PriceWithInfo { Amount = 1000000,Currency="USD",Info="One MILLION dollars!"}
            };

            display.RentSurfaces = new[]
            {
                new RentSurface {MaxArea = 500, MinArea = 10, Type = PremiseSurfaceType.Office},
                new RentSurface {MaxArea = 300, MinArea = 20, Type = PremiseSurfaceType.Shop}
            };

            //Only DisplayMode.Public and DisplayMode.Private are used
            display.Status = DisplayMode.Public;

            //Legacy stuff for multiple types, no longer supported. InternetDisplay should be an array with only one element of type Premise in it.
            e.InternetDisplay = new InternetDisplay[] {display};
            #endregion

            return e;
        }

        static void Log()
        {
            Console.WriteLine();
        }
        static void Log(string input)
        {
            Console.WriteLine(input);
        }

        static bool Login()
        {
            var settings = new SessionSettings();
            return server.Login("Objektvision TestClient", brokerId, password, settings);
        }

        static Estate GetEstate(int serverId)
        {
            return server.GetEstateByServerID(serverId, brokerId, password);
        }
    }
}
