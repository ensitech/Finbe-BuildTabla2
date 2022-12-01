using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AltaCredito;
using Microsoft.Xrm.Sdk;
using System.ServiceModel.Description;
using Microsoft.Xrm.Sdk.Client;
namespace buildTabla2
{
    /// <summary>
    /// Descripción resumida de UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        public UnitTest1()
        {
            //
            // TODO: Agregar aquí la lógica del constructor
            //Entity entity = null;
            buildTabla2.initTabla plugin = new buildTabla2.initTabla();
            Entity mainTable = new Entity("fib_configamortiza");
            plugin.setTable(buildTabla2.CRMLogin.createService(), new Guid("379B73EF-45CB-E911-80A5-005056BF349E"), mainTable);
            //AltaCredito.InsertCredito plugin = new AltaCredito.InsertCredito();
            //plugin.insertInAx(new Guid("C8ABAC39-D5FE-E611-8CE8-0050569D182E"), new Guid("FF6A9E73-36D7-E511-9D28-0050569D182E"), CRMLogin.createService());
            //8C24C39D-CD1A-E511-8FA4-0050569D182E "954241FE-C71A-E511-8FA4-0050569D182E"//954241fe-c71a-e511-8fa4-0050569d182e
            //38D3FAB5-2692-E511-BB05-005056851F55
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Obtiene o establece el contexto de las pruebas que proporciona
        ///información y funcionalidad para la ejecución de pruebas actual.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Atributos de prueba adicionales
        //
        // Puede usar los siguientes atributos adicionales conforme escribe las pruebas:
        //
        // Use ClassInitialize para ejecutar el código antes de ejecutar la primera prueba en la clase
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup para ejecutar el código una vez ejecutadas todas las pruebas en una clase
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Usar TestInitialize para ejecutar el código antes de ejecutar cada prueba 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup para ejecutar el código una vez ejecutadas todas las pruebas
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestMethod1()
        {
            //
            // TODO: Agregar aquí la lógica de las pruebas
            //
        }
    }
}
