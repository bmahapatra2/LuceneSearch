using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene;
using Lucene.Net.Store;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;

namespace LuceneSearchDemo
{
    public class Flight
    {

        public int FlightId { get; set; }
        public string FlightName { get; set; }
        public string FlightDestination { get; set; }

    }
    public static partial class DataRepository
    {
        public static Flight Get(int id)
        {
            return GetAllFlight().SingleOrDefault(x => x.FlightId.Equals(id));
        }
        public static List<Flight> GetAllFlight()
        {
            return new List<Flight>
            {
                new Flight {FlightId = 1, FlightName = "Air India", FlightDestination = "Serbia"},
                new Flight {FlightId = 2, FlightName = "Jet Airways", FlightDestination = "Russia"},
                new Flight {FlightId = 3, FlightName = "Indigo", FlightDestination = "USA"},
                new Flight {FlightId = 4, FlightName = "Emirats", FlightDestination = "India"},
                new Flight {FlightId = 5, FlightName = "Lufthansa", FlightDestination = "Hong-Kong"},
            };
        }

    }
    public static partial class DataRepository
    {
        public static class LuceneSearch
        {
            private static string _luceneDir = @"D:\LuceneDemo\LuceneSearchDemo\Log";
            private static FSDirectory _directoryTemp;
            private static FSDirectory _directory
            {
                get
                {
                    if (_directoryTemp == null) _directoryTemp = FSDirectory.Open(new DirectoryInfo(_luceneDir));
                    if (IndexWriter.IsLocked(_directoryTemp)) IndexWriter.Unlock(_directoryTemp);
                    var lockFilePath = Path.Combine(_luceneDir, "write.lock");
                    if (File.Exists(lockFilePath)) File.Delete(lockFilePath);
                    return _directoryTemp;
                }
            }

            private static void _addToLuceneIndex(Flight flight, IndexWriter writer)
            {
                // remove older index entry
                var searchQuery = new TermQuery(new Term("FlightId", flight.FlightId.ToString()));
                writer.DeleteDocuments(searchQuery);

                // add new index entry
                var doc = new Document();

                // add lucene fields mapped to db fields
                doc.Add(new Field("FlightId", flight.FlightId.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field("FlightName", flight.FlightName, Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field("FlightDestination", flight.FlightDestination, Field.Store.YES, Field.Index.ANALYZED));

                // add entry to index
                writer.AddDocument(doc);
            }

            public static void AddUpdateLuceneIndex(IEnumerable<Flight> sampleDatas)
            {
                // init lucene
                var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    // add data to lucene search index (replaces older entry if any)
                    foreach (var sampleData in sampleDatas) _addToLuceneIndex(sampleData, writer);

                    // close handles
                    analyzer.Close();
                    writer.Dispose();
                }
            }

            private static Flight _mapLuceneDocumentToData(Document doc)
            {
                return new Flight
                {
                    FlightName = doc.Get("FlightName"),
                    FlightId = int.Parse(doc.Get("FlightId")),
                    FlightDestination = doc.Get("FlightDestination")
                };
            }
            private static IEnumerable<Flight> _mapLuceneToDataList(IEnumerable<Document> hits)
            {
                return hits.Select(_mapLuceneDocumentToData).ToList();
            }
            private static IEnumerable<Flight> _mapLuceneToDataList(IEnumerable<ScoreDoc> hits,
                IndexSearcher searcher)
            {
                return hits.Select(hit => _mapLuceneDocumentToData(searcher.Doc(hit.Doc))).ToList();
            }
            private static Query parseQuery(string searchQuery, QueryParser parser)
            {
                Query query;
                try
                {
                    query = parser.Parse(searchQuery.Trim());
                }
                catch (ParseException)
                {
                    query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
                }
                return query;
            }
            public static IEnumerable<Flight> Search
             (string searchQuery, string searchField = "")
            {
                // validation
                if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", ""))) return new List<Flight>();

                // set up lucene searcher
                using (var searcher = new IndexSearcher(_directory, false))
                {
                    var hits_limit = 1000;
                    var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

                    // search by single field
                    if (!string.IsNullOrEmpty(searchField))
                    {
                        var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, searchField, analyzer);
                        var query = parseQuery(searchQuery, parser);
                        var hits = searcher.Search(query, hits_limit).ScoreDocs;
                        var results = _mapLuceneToDataList(hits, searcher);
                        analyzer.Close();
                        searcher.Dispose();
                        return results;
                    }
                    // search by multiple fields (ordered by RELEVANCE)
                    else
                    {
                        var parser = new MultiFieldQueryParser
                        (Lucene.Net.Util.Version.LUCENE_30, new[] { "FlightName", "FlightId", "FlightDestination" }, analyzer);
                        var query = parseQuery(searchQuery, parser);
                        var hits = searcher.Search
                        (query, null, hits_limit, Sort.RELEVANCE).ScoreDocs;
                        var results = _mapLuceneToDataList(hits, searcher);
                        analyzer.Close();
                        searcher.Dispose();
                        return results;
                    }
                }
            }
        }

    }
}
