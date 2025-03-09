using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Dynamic;
using System.Collections.Generic;  // Needed for IDictionary
using System.Text.RegularExpressions;  // Needed for Regex

namespace WpfMongoJsonApp
{
    public class Record
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Lore { get; set; }
        public string Alignments { get; set; }
        public double Chance { get; set; }
        public string Class { get; set; }
    }

    public partial class MainWindow : Window
	{
        public ObservableCollection<Record> Records { get; set; } = new ObservableCollection<Record>();
        private readonly string jsonFilePath = "C:\\path\\to\\yourfile.json"; // Adjust the file path
		private IMongoCollection<BsonDocument> mongoCollection; // MongoDB collection variable

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;

			// Initialize MongoDB client and collection (default for now)
			var client = new MongoClient("mongodb://localhost:27017");
			var database = client.GetDatabase("qb");
			mongoCollection = database.GetCollection<BsonDocument>("deity"); // Default collection (adjust as needed)
		}

		private void LoadFromMongoDB()
		{
			try
			{
				// Check if DB path is valid
				if (Regex.IsMatch(txtDBPath.Text, "@(?:[\\d+\\.]+|localhost)\\:\\d+") && !string.IsNullOrEmpty(txtDBPath.Text)) // Check if the path is valid 
				{
                    var records = mongoCollection.Find(new BsonDocument()).ToList();
                    Records.Clear();

                    foreach (var record in records)
                    {
                        Records.Add(new Record
                        {
                            Id = record["_id"].ToString(),
                            Name = record.GetValue("name", "").ToString(),
                            Lore = record.GetValue("lore", "").ToString(),
                            Alignments = record.GetValue("alignments", "").ToString(),
                            Chance = record.GetValue("chance", 0).ToDouble(),
                            Class = record.GetValue("class", "").ToString()
                        });
                    }
                }

			// Ensure mongoCollection is initialized
				if (mongoCollection != null)
				{
					var records = mongoCollection.Find(new BsonDocument()).ToList();  // Fetch all records

					Records.Clear();
					foreach (var record in records)
					{
						dynamic obj = new ExpandoObject();
						var dict = (IDictionary<string, object>)obj;

						foreach (var element in record.Elements)
						{
							dict[element.Name] = BsonTypeMapper.MapToDotNetValue(element.Value);
						}

						Records.Add(obj); // Add the record to the ObservableCollection
					}
				}
				else
				{
					MessageBox.Show("MongoDB collection is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			} 
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadFromJson()
		{
			// Check if the JSON file exists
			if (File.Exists(jsonFilePath))
			{
                string json = File.ReadAllText(jsonFilePath);
                var records = JsonSerializer.Deserialize<List<Record>>(json);

                Records.Clear();
                foreach (var record in records)
                {
                    Records.Add(record);
                }
            }
			else
			{
				MessageBox.Show("JSON file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Load MongoDB data when button is clicked
		private void Button_LoadMongo_Click(object sender, RoutedEventArgs e)
		{
			LoadFromMongoDB();
		}

		// Load JSON data when button is clicked
		private void Button_LoadJson_Click(object sender, RoutedEventArgs e)
		{
			LoadFromJson();
		}
	}
}
