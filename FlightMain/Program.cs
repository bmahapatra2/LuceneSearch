using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuceneSearchDemo;
using static LuceneSearchDemo.DataRepository;

namespace FlightMain
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Flight> flightList = DataRepository.GetAllFlight();
            LuceneSearch.AddUpdateLuceneIndex(flightList);
            while (true)
            {
                Console.WriteLine("Enter your search query...");
                string querry = Console.ReadLine();
                IEnumerable<Flight> FlightResult = LuceneSearch.Search(querry);
                if (FlightResult.Count<Flight>() > 0)
                {
                    Console.WriteLine($"{FlightResult.Count<Flight>()} Record(s) Found\n");
                    int i = 1;
                    foreach (Flight flight in FlightResult)
                    {
                        Console.WriteLine($"Record ({i++})\n");
                        Console.WriteLine($"FlightName: {flight.FlightName}\n Flight Id:  {flight.FlightId}\nFlight Description: {flight.FlightDestination}");
                    }
                }
                else Console.WriteLine("No such record found");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }
}
