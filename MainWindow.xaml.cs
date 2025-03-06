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
	public partial class MainWindow : Window
	{
		public ObservableCollection<ExpandoObject> Records { get; set; } = new ObservableCollection<ExpandoObject>();
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
					var client = new MongoClient("mongodb://" + txtDBPath.Text);
					if (txtDBName.Text != "Database" && !string.IsNullOrEmpty(txtDBName.Text) && txtDBColl.Text != "Collection" && !string.IsNullOrEmpty(txtDBColl.Text))
					{
						var database = client.GetDatabase(txtDBName.Text);
						mongoCollection = database.GetCollection<BsonDocument>(txtDBColl.Text); // Set collection dynamically based on input
					}
					else
					{
						MessageBox.Show("Database and collection name are required.", "MongoDB Database", MessageBoxButton.OK, MessageBoxImage.Information);
						return;
					}
				}

			// Ensure mongoCollection is initialized
				if (mongoCollection != null)
				{
					var records = mongoCollection.Find(new BsonDocument()).ToList();  // Fetch all records

					Records.Clear();
					foreach (var record in records)
					{
						dynamic expando = new ExpandoObject();
						foreach (var element in record.Elements)
						{
							((IDictionary<string, object>)expando)[element.Name] = BsonTypeMapper.MapToDotNetValue(element.Value);
						}
						Records.Add(expando);  // Add the record to the ObservableCollection
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
				string json = File.ReadAllText(jsonFilePath); // Read JSON file
				var jsonArray = JsonNode.Parse(json)?.AsArray();
				Records.Clear();

				if (jsonArray != null)
				{
					foreach (var jsonObj in jsonArray)
					{
						dynamic expando = new ExpandoObject();
						if (jsonObj is JsonObject obj)
						{
							foreach (var kvp in obj)
							{
								((IDictionary<string, object>)expando)[kvp.Key] = kvp.Value?.ToString();
							}
						}
						Records.Add(expando);
					}
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
