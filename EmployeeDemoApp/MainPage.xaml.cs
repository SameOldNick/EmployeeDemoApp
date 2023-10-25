using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace EmployeeDemoApp
{
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Stores all employees
        /// </summary>
        public List<Employee> AllEmployees { get; set; }

        /// <summary>
        /// Filtered list of employees to display in window.
        /// </summary>
        public ObservableCollection<Employee> DisplayedEmployees { get; set; }

        /// <summary>
        /// Statuses for picker
        /// </summary>
        public ObservableCollection<string> Statuses { get; set; }

        /// <summary>
        /// Gets the full path to the CSV file.
        /// </summary>
        public string CSVFilePath
        {
            get
            {
                /**
                 * Just using File.ReadAllLines("Data\\employees.csv") will result in trying to read the file from C:\Windows\System32\....
                 * Instead, we need to get the directory for the running executable and concat "Data\employees.csv" to that.
                 */
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(currentDir, "Data\\employees.csv");

                return filePath;
            }
        }

        /// <summary>
        /// Gets the full path to the JSON file.
        /// </summary>
        public string JSONFilePath
        {
            get
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(currentDir, "Data\\employees.json");

                return filePath;
            }
        }

        /// <summary>
        /// Constructs the main page
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            this.AllEmployees = new List<Employee>();
            this.DisplayedEmployees = new ObservableCollection<Employee>();
            this.Statuses = new ObservableCollection<string>();

            /**
             * Required!
             * Tells components to use this instance of MainPage to pull and push data from.
             */
            this.BindingContext = this;

            // Hook on to destroying event (fires when app is closing)
            App.Current.MainPage.Window.Destroying += Window_Destroying;

            // Populates employees and statuses.
            this.LoadEmployeesFromFile();
            this.PopulateStatuses();

            // Display all employees
            this.RefreshEmployees();
        }
        

        /// <summary>
        /// Called before the app is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Destroying(object sender, EventArgs e)
        {
            // Save employees to file before app closes.
            this.SaveEmployeesToFile();
        }

        /// <summary>
        /// Loads employees from file.
        /// </summary>
        private void LoadEmployeesFromFile()
        {
            this.AllEmployees.Clear();

            // Check if JSON file exists
            if (File.Exists(this.JSONFilePath))
            {
                // Load file contents
                string contents = File.ReadAllText(this.JSONFilePath);

                // Decode it from JSON
                // The type must be exactly the same that was used for serializing.
                object employeesObj = JsonSerializer.Deserialize(contents, this.AllEmployees.GetType());

                List<Employee> employees = employeesObj as List<Employee>;

                // Ensure JSON data was decoded as List<Employee> type
                if (employees == null) 
                {
                    this.DisplayAlert("Error", "Unable to decode from JSON file.", "OK");
                    return;
                }

                // Add all employees from JSON file
                foreach (Employee employee in employees)
                {
                    this.AllEmployees.Add(employee);
                }
            } 
            else
            {
                // If not, load from CSV file.
                string[] lines = File.ReadAllLines(this.CSVFilePath);

                foreach (string line in lines)
                {
                    string[] columns = line.Split(',');

                    int id = int.Parse(columns[0]);
                    string name = columns[1];
                    bool isActive = bool.Parse(columns[2]);

                    Employee employee = new Employee(id, name, isActive);

                    this.AllEmployees.Add(employee);
                }
            }

            
        }

        /// <summary>
        /// Populates statuses for picker.
        /// </summary>
        private void PopulateStatuses()
        {
            this.Statuses.Clear();

            this.Statuses.Add("Both");
            this.Statuses.Add("Active");
            this.Statuses.Add("Inactive");
        }

        /// <summary>
        /// Saves all employees back to file.
        /// </summary>
        private void SaveEmployeesToFile()
        {   
            string encoded = JsonSerializer.Serialize(this.AllEmployees, this.AllEmployees.GetType());

            File.WriteAllText(this.JSONFilePath, encoded);

        }

        /// <summary>
        /// Refreshes the displayed employees listview.
        /// </summary>
        private void RefreshEmployees()
        {
            this.DisplayedEmployees.Clear();

            foreach (Employee emp in this.AllEmployees)
            {
                this.DisplayedEmployees.Add(emp);
            }
        }

        /// <summary>
        /// Called when user clicks "Find" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FindButton_Clicked(object sender, EventArgs e)
        {
            // Figure out what we're finding.
            int expectedId;
            string expectedName;
            string expectedStatus;

            /*
             * "string.IsNullOrEmpty(str)" is the same as writing "str == null || str.Length == 0"
             */
            if (string.IsNullOrEmpty(this.findEmployeeIdEntry.Text) == false)
            {
                expectedId = int.Parse(this.findEmployeeIdEntry.Text);
            }
            else
            {
                expectedId = 0;
            }

            if (string.IsNullOrEmpty(this.findEmployeeNameEntry.Text) == false)
            {
                expectedName = this.findEmployeeNameEntry.Text;
            }
            else
            {
                expectedName = "";
            }

            expectedStatus = (string)this.findEmployeeStatusPicker.SelectedItem;

            // If field is filled in, we'll find that.
            bool findEmployeesWithId = false;
            bool findEmployeesWithName = false;
            bool findEmployeesWithStatus = false;

            if (expectedId > 0)
            {
                findEmployeesWithId = true;
            }

            if (expectedName.Length > 0)
            {
                findEmployeesWithName = true;
            }

            if (expectedStatus != "Both")
            {
                findEmployeesWithStatus = true;
            }

            /**
             * Create new empty list of employees.
             * This will be populated with the list of filtered employees and then replace the existing DisplayedEmployees property value.
             */
            ObservableCollection<Employee> found = new ObservableCollection<Employee>();

            /**
             * Filter employees:
             * This does a reverse search, meaning it first assumes everything matches and
             * then goes backwards and checks which fields don't actually match.
             */
            foreach (Employee employee in this.AllEmployees) {
                bool idMatches = true;
                bool nameMatches = true;
                bool statusMatches = true;

                // Create variables from employee object.
                int actualId = employee.Id;
                string actualName = employee.Name;
                bool actualStatus = employee.IsActive;

                // Check if trying to find ID and if ID doesn't match expected ID.
                if (findEmployeesWithId && actualId != expectedId)
                {
                    idMatches = false;
                }

                // Check if trying to find name and if name doesn't contain expected name.
                if (findEmployeesWithName && !actualName.Contains(expectedName))
                {
                    nameMatches = false;
                }

                // Check if trying to find status
                if (findEmployeesWithStatus)
                {
                    if (expectedStatus == "Active" && actualStatus == false)
                    {
                        statusMatches = false;
                    }

                    if (expectedStatus == "Inactive" && actualStatus == true)
                    {
                        statusMatches = false;
                    }
                }

                // Add employee if matches
                if (idMatches && nameMatches && statusMatches)
                {
                    found.Add(employee);
                }
            }

            // Replace Employees list with found employees.
            // We cannot just simply assign DisplayedEmployees the existing list, we have to copy each object over into the other list.
            this.DisplayedEmployees.Clear();

            foreach (Employee employee in found)
            {
                this.DisplayedEmployees.Add(employee);
            }
        }

        /// <summary>
        /// Called when user clicks the "Add" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Clicked(object sender, EventArgs e)
        {
            // Get input field values
            int id;
            string name;
            string status;

            if (string.IsNullOrEmpty(this.addEmployeeIdEntry.Text) == false)
            {
                id = int.Parse(this.addEmployeeIdEntry.Text);
            } 
            else
            {
                id = -1;
            }

            if (string.IsNullOrEmpty (this.addEmployeeNameEntry.Text) == false)
            {
                name = this.addEmployeeNameEntry.Text;
            } 
            else
            {
                name = "";
            }

            status = (string)this.addEmployeeStatusPicker.SelectedItem;

            // Validate values

            // Check if ID is negative or 0 tell user it's invalid and stop!
            if (id <= 0)
            {
                DisplayAlert("Ooops!", "The ID cannot be 0 or negative.", "OK");
                return;
            }

            // Check if a name was entered.
            if (string.IsNullOrEmpty(name))
            {
                DisplayAlert("Ooops!", "The name cannot be empty.", "OK");
                return;
            }

            // Check if a status was selected.
            if (string.IsNullOrEmpty(status))
            {
                DisplayAlert("Ooops!", "The status must be selected.", "OK");
                return;
            }

            // Add employee
            bool statusBool = status == "Active" ? true : false;

            Employee employee = new Employee(id, name, statusBool);

            this.AllEmployees.Add(employee);

            // Tell user employee was added
            this.DisplayAlert("Success!", "Employee was added.", "OK");

            // Display list with new employee
            this.RefreshEmployees();
        }
    }
}