using RandomPasswordGenerator.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RandomPasswordGenerator.Controllers
{
    public class HomeController : Controller
    {

        public const string CapsLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string lowLetters = "abcdefghijklmnopqrstuvwxyz";
        public const string numb = "1234567890";
        public const string spec = "!@#$%^&*()_+";
        public const string combination = CapsLetters + lowLetters + numb + spec;
        HashSet<string> mySet;
        public readonly string CS = ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString;
        public int PasswordLength = 8;
        List<User> userlist;
        /// <summary>
        /// Fetches User List and will display Users on the screen
        /// </summary>
        /// <param name="sortBy">Sorts user in Asc and Desc order</param>
        /// <param name="searchString">Searching particular user</param>
        /// <returns></returns>
        public ViewResult Index(string sortBy, string searchString)
        {            
            ViewData["DateSortParm"] = sortBy == "asc" ? "desc" : "asc";
            //DB call method to fetch user list
            userlist = fetchUser();   
            if (!String.IsNullOrEmpty(searchString))
            {
                userlist = userlist.Where(s => s.userName.Contains(searchString)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy)
                {
                    case "asc":
                        userlist = userlist.OrderBy(x => x.userName).ToList();
                        break;
                    case "desc":
                        userlist = userlist.OrderByDescending(x => x.userName).ToList();
                        break;
                }
            }
            //Saving value in TempData to be accessed into Generate Action and only those which are currently fetched will get password generate.
            TempData["userlist"] = userlist;
            return View(userlist);
        }

        /// <summary>
        /// View to verify of all the users have been assigned a new password.
        /// ** Such Processing should only take place if the ount of users are less if not then should been looked into a much optimized solution
        /// Like saving such password in a batch which will reduce the processing while assigning the values
        /// Minimum of 4 such batch should be present in the DB so that processing is not repeated multiple times and reduce the load on server processing.
        /// </summary>
        /// <returns></returns>
        public ActionResult Generate()
        {
            var userList = (List<User>)TempData["userlist"];
            mySet = new HashSet<string>();
            foreach (var user in userList)
            {
                var password = generatePassword(PasswordLength);
                if(password != null && password.Length == PasswordLength)
                {
                    user.password = password;
                }                
            }
            TempData["userlist"] = userList;     
            return View(userList);
        }

        /// <summary>
        /// Saves the value into DB with new password.
        /// </summary>
        /// <returns></returns>
        public ActionResult Save()
        {
            var userList = (List<User>) TempData["userlist"];
            if(userList.Count > 0)
            {
                using (SqlConnection con = new SqlConnection(CS))
                {
                    try
                    {
                        con.Open();
                        string updateCMD = "UPDATE [dbo].[User] SET Password = '{0}' WHERE Id = {1}";
                        foreach (var user in userList)
                        {
                            var usercmd = string.Format(updateCMD, user.password, user.Id);
                            SqlCommand cmd = new SqlCommand(usercmd, con);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        throw ex;
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }           
            return RedirectToAction("Index");
        }

        /// <summary>
        /// DB query to fetch List of Users 
        /// </summary>
        /// <returns>List of Users</returns>
        public List<User> fetchUser()
        {
            userlist = new List<User>();
            mySet = new HashSet<string>();
            using (SqlConnection con = new SqlConnection(CS))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("SELECT * FROM [dbo].[User]", con);
                    cmd.CommandType = CommandType.Text;
                    con.Open();

                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        var user = new User();
                        user.Id = Convert.ToInt32(rdr["Id"]);
                        user.userName = rdr["UserName"].ToString();
                        user.password = rdr["Password"].ToString();                                                
                        userlist.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
            }
            return userlist;
        }

        /// <summary>
        /// Generate Password Method Helps to Generate password according to our need.
        /// Currently i have kept one element from each set is needed this will help to create complex password
        /// </summary>
        /// <param name="PasswordLength"></param>
        /// <returns>Randomly Generate Password</returns>
        public string generatePassword(int PasswordLength)
        {            
            if(PasswordLength > 0)
            {
                //Since random value will return same value in quick succession it needs to be assigned a seed value.
                Random random = new Random(Guid.NewGuid().GetHashCode());
                char[] password = new char[PasswordLength];
                //Specially to have atleast one of each set of values
                password[0] = lowLetters[(random.Next(lowLetters.Length))];
                password[1] = CapsLetters[(random.Next(CapsLetters.Length))];
                password[2] = numb[(random.Next(numb.Length))];
                password[3] = spec[(random.Next(spec.Length))];
                //Filling up rest of the password with random value
                for (int i = 4; i < PasswordLength; i++)
                {
                    password[i] = combination[random.Next(combination.Length)];
                }
                var generatedPassword = new string(password);
                //Validate if the password was already created from current set of users.
                if (mySet == null && mySet.Count > 0 && mySet.Contains(generatedPassword))
                {
                    generatePassword(PasswordLength);
                }
                return generatedPassword;
            }
            return null;
        }
    }
}