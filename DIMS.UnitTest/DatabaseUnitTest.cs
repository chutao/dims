using System.Diagnostics;

namespace DIMS.UnitTest;

[TestClass]
public class DatabaseUnitTest
{
    private static readonly string ConnectionString = "server=127.0.0.1;port=33060;uid=root;pwd=admin;database=test";
    private Helpers.MysqlDbHelper helper = Helpers.MysqlDbHelper.Default;

    public DatabaseUnitTest()
    {
        helper.SetConnectionString(ConnectionString);
    }

    [TestMethod]
    public void TestHistory()
    {
        // Insert
        var model = new Models.ProductionDataModel();
        model.Timestamp = DateTime.Now - TimeSpan.FromMinutes(10);
        model.ProductCode = "123456";
        model.TrayCode = "abc";

        var result = helper.HistoryInsert(model);
        Assert.IsTrue(result);

        // Query
        var query = helper.HistoryQuery(DateTime.Now - TimeSpan.FromHours(1), DateTime.Now, null, null);
        Assert.IsTrue(query != null && query.Count() > 0);
        model.Id = query.ElementAt(0).Id;
        Debug.WriteLine($"Queried element id is " + model.Id);

        // Update
        model.State = 10;
        result = helper.HistoryUpdate(model);
        Assert.IsTrue(result);

        // Delete
        result = helper.HistoryDelete((int)model.Id);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TestProduct()
    {
        helper.ProductDelete("123456");
        
        // Insert
        var model = new Models.ProductDataModel();
        model.POSCode = "123456";
        model.Model = "ND105";
        model.CodeLength = 18;
        model.Category = 1;

        var result = helper.ProductInsert(model);
        Assert.IsTrue(result);

        // Update
        model.Model = "MD103";
        result = helper.ProductUpdate(model);
        Assert.IsTrue(result);

        // Query
        var query = helper.ProductQuery("123456", null);
        Assert.IsTrue(query != null && query.Count() > 0);

        // Delete
        //result = helper.ProductDelete("123456");
        //Assert.IsTrue(result);
    }
}