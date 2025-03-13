using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;  // For DataGrid, DataGridAutoGeneratingColumnEventArgs, DataGridTextColumn, ToolTip 
using System.Windows.Data;      // For Binding
using MongoDB.Bson;
using MongoDB.Driver;           // Both needed for MongoDB 
using System.Text.RegularExpressions;  // Needed for Regex

namespace WpfMongoJsonApp
{
	public class Record
	{
		public string? Id { get; set; }  // Nullable to avoid warnings
		public string? Name { get; set; }
		public string? Lore { get; set; }
		public string? Alignments { get; set; }
		public double? Chance { get; set; }  // Nullable to prevent warnings
		public string? Class { get; set; }
	}

	public partial class MainWindow : Window
	{
		public ObservableCollection<Record> Records { get; set; } = new ObservableCollection<Record>();
		private readonly string jsonFilePath = "C:\\path\\to\\yourfile.json"; // Adjust the file path
		private IMongoCollection<BsonDocument> mongoCollection; // MongoDB collection variable

		private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
			// Check if the column is "Chance" and format it
			if (e.PropertyName == "Chance")
			{
				if (e.Column is DataGridTextColumn textColumn)
				{
					// Apply number formatting for double values
					textColumn.Binding = new Binding(e.PropertyName)
					{
						StringFormat = "F5"  // 5 decimal places (adjustable :3) 
					};
				}
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;

			// Initialize MongoDB client and collection (default for now)
			var client = new MongoClient("mongodb://localhost:27017");
			var database = client.GetDatabase("qb");
			mongoCollection = database.GetCollection<BsonDocument>("deity"); // Default collection (adjust as needed)
		}

		private void showBtnCls()
		{
			btnCls.Visibility = Visibility.Visible;
		}

		public void SetMinWidths(object source, EventArgs e)
		{
			Griddy.Width = MinWidth;
			foreach (var column in Griddy.Columns)
			{
				column.MinWidth = column.ActualWidth;
				column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
			}
		}

		private void ClearDataGrid()
		{
			Records.Clear();
			btnCls.Visibility = Visibility.Collapsed;
			Application.Current.MainWindow.Width = MinHeight;
			Application.Current.MainWindow.Height = MinWidth;
			Griddy.Loaded += SetMinWidths;
		}

		private void LoadFromMongoDB()
		{
			try
			{
				if (Regex.IsMatch(txtDBPath.Text, "@(?:[\\d+\\.]+|localhost)\\:\\d+") && !string.IsNullOrEmpty(txtDBPath.Text))
				{
					var client = new MongoClient("mongodb://" + txtDBPath.Text);
					if (!string.IsNullOrEmpty(txtDBName.Text) && txtDBName.Text != "Database" &&
						!string.IsNullOrEmpty(txtDBColl.Text) && txtDBColl.Text != "Collection")
					{
						var database = client.GetDatabase(txtDBName.Text);
						mongoCollection = database.GetCollection<BsonDocument>(txtDBColl.Text);
					}
					else
					{
						MessageBox.Show("Database and collection name are required.", "MongoDB Error", MessageBoxButton.OK, MessageBoxImage.Information);
						return;
					}
				}

				if (mongoCollection == null)
				{
					MessageBox.Show("MongoDB collection is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				var records = mongoCollection.Find(new BsonDocument()).ToList();
				Records.Clear();

				foreach (var bsonDoc in records)
				{
					var newRecord = new Record
					{
						Id = bsonDoc.Contains("_id") ? bsonDoc["_id"].ToString() : "Unknown",
						Name = bsonDoc.Contains("name") ? bsonDoc["name"].ToString() : "Unknown",
						Lore = bsonDoc.Contains("lore") ? bsonDoc["lore"].ToString() : "Unknown",
						Alignments = bsonDoc.Contains("alignments") ? bsonDoc["alignments"].ToString() : "None",
						Chance = bsonDoc.Contains("chance") ? (double?)bsonDoc["chance"].ToDouble() : null,  // FIX HERE
						Class = bsonDoc.Contains("class") ? bsonDoc["class"].ToString() : "Unknown"
					};

					Records.Add(newRecord);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading from MongoDB: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			showBtnCls();
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

			showBtnCls();
		}

		// Clear the DataGrid when button is clicked and then hide said button
		private void Button_CLS_Click(object sender, RoutedEventArgs e)
		{
			ClearDataGrid();
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
