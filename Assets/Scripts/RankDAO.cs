using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    class RankDAO
    {
        static string dbconfig = "server=49.235.3.103;port=3306;user=root;password=2382525abc;Database=db_general;Charset=utf8";
        public static string selectFromDb()
        {
            MySqlConnection conn = new MySqlConnection(dbconfig);
            string rank = "";
            int no = 1;
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM tb_test ORDER BY score DESC", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rank += string.Format("{0, 15}{1, 16}{2, 12}\n", no, reader["id"].ToString().Trim(), reader["score"].ToString().Trim());
                    no++;
                }
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }
            return rank;
        }

        public static void insertIntoTable(string id, int score)
        {
            MySqlConnection conn = new MySqlConnection(dbconfig);
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("INSERT INTO tb_test(id, score) VALUES(@id, @score)", conn);
                cmd.Prepare();
                /*Byte[] rowBytes = Encoding.Default.GetBytes(id);
                id = Encoding.UTF8.GetString(rowBytes);*/
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@score", score);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }
        }
    }
}
