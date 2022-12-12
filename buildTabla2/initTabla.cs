using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System.Web.Services.Protocols;
using System.Globalization;
using System.Numeric;
using System.Diagnostics;

namespace buildTabla2
{
    public class initTabla : IPlugin
    {
        private List<Entity> PagosGen;
        private string result = "Exito";
        private bool pgirregular = false;
        private DateTime ultpagofecha;

        public initTabla() { }

        public void Execute(IServiceProvider serviceProvider)
        {

            Entity entity = null;
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory ICrm = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService servicio = ICrm.CreateOrganizationService(context.UserId);

            PConsole.init("buildTabla2", "10.20.252.11", 12300, false);
            PConsole.writeLine("INICIA");

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                result = "Exito";
                entity = (Entity)context.InputParameters["Target"];

                PConsole.writeLine("INICIA IF: " + (!(entity.LogicalName == "fib_configamortiza2") || !(entity.LogicalName == "fib_configamortiza")).ToString() + " " + entity.LogicalName);

                //Se doble condiciona para que se ejecute tanto en la disposicion como en el cotizador.
                /*if (!(entity.LogicalName == "fib_configamortiza2") || !(entity.LogicalName == "fib_configamortiza"))
                {
                    return;
                }*/
            }
            else
                return;

            PConsole.writeLine("INICIA 1");
            Guid idta = new Guid(((Entity)context.InputParameters["Target"]).Attributes[context.PrimaryEntityName + "id"].ToString());
            PConsole.writeLine("INICIA 1: " + (entity.Attributes.Contains("fib_cotizacionid") ? "COTIZADOR" : "DISPOSICION"));
            setTable(servicio, idta, entity);
            PConsole.writeLine("FINALIZA");
        }

        public void setTable(IOrganizationService servicio, Guid idta, Entity entity)
        {
            try
            {
                PConsole.writeLine("setTable 0");
                DateTime NullDate = DateTime.MinValue;
                bool anualidad = false;
                string entityName = entity.LogicalName;

                //Entity mainTable = new Entity("fib_configamortiza2");
                Entity mainTable = new Entity(entity.LogicalName);
                PConsole.writeLine("setTable 1: " + idta.ToString());

                //mainTable = fib_configamortiza2;
                mainTable = entity;


                //por pruebas
                //mainTable = servicio.Retrieve("fib_configamortiza2", idta, new ColumnSet(true));

                //EntityReference fib_cotizadorid = (EntityReference)mainTable.Attributes[((entityName == "fib_configamortiza") ? "fib_disposicioncreditoid" : "fib_cotizacionid")];
                EntityReference ProcesarId = (EntityReference)mainTable.Attributes[((entityName == "fib_configamortiza") ? "fib_disposicioncreditoid" : "fib_cotizacionid")];
                PConsole.writeLine("setTable 2: " + ProcesarId.Id);
                clearOldConfigs(servicio, ProcesarId.Id, idta, entityName);
                clearOldsPeriodos(servicio, ProcesarId.Id, entityName);

                PConsole.writeLine("setTable 2.5: " + mainTable.Attributes["fib_codigoperiodicidad"].ToString());
                Entity periodo = new Entity("fib_catalogoperiododepago");
                periodo = getPeriodo(servicio, new Guid(mainTable.Attributes["fib_codigoperiodicidad"].ToString()));

                Entity calcpago = new Entity("fib_catalogodecalculodepago");
                //calcpago = getCalcPago(servicio, new Guid(mainTable.Attributes["fib_fib_cdigocc"].ToString()));
                calcpago = getCalcPago(servicio, new Guid(mainTable.Attributes[((entityName == "fib_configamortiza") ? "fib_cdigocc" : "fib_fib_cdigocc")].ToString()));

                anualidad = (calcpago.Attributes["fib_name"].ToString().ToUpper().Contains("ANUALIDAD") || calcpago.Attributes["fib_name"].ToString().ToUpper().Contains("ÚNICO") || calcpago.Attributes["fib_name"].ToString().ToUpper().Contains("IRREGULAR"));
                pgirregular = calcpago.Attributes["fib_name"].ToString().ToUpper().Contains("ANUALIDAD");

                GenAmortiza TablaAmortiza = new GenAmortiza();
                DateTime dateGracia = (mainTable.Attributes.Contains("fib_periododegracia")) ? Convert.ToDateTime(mainTable.Attributes["fib_periododegracia"].ToString().ToString().Substring(0, 19)) : NullDate;
                if (dateGracia != NullDate)
                    dateGracia = DateTime.Parse(dateGracia.ToString("s"));
                PConsole.writeLine("setTable 3: " + dateGracia);

                //DateTime dateInicio = DateTime.Parse(mainTable.Attributes["fib_fechadeinicio"].ToString().ToString().Substring(0,19), System.Globalization.CultureInfo.InvariantCulture);
                DateTime dateInicio = Convert.ToDateTime(mainTable.Attributes["fib_fechadeinicio"].ToString().ToString().Substring(0, 19));
                PConsole.writeLine("setTable 4: " + dateInicio);

                Money fib_monto = (Money)mainTable.Attributes["fib_monto"];
                PConsole.writeLine("setTable 5: " + double.Parse(fib_monto.Value.ToString()).ToString());

                dateInicio = DateTime.Parse(dateInicio.ToString("s"));

                #region Para Seguro Multianual 2019

                //EntityReference fib_tablaamortizacinid = (EntityReference)mainTable.Attributes[((entityName == "fib_configamortiza") ? "fib_disposicioncreditoid" : "fib_cotizacionid")];
                Seguro infoSeguro = getTiposeguro(entity);

                #endregion

                TablaAmortiza.configTable(Int32.Parse(periodo.Attributes["fib_equivalenciaendias"].ToString().ToString()), anualidad,
                    double.Parse(fib_monto.Value.ToString()), double.Parse(mainTable.Attributes["fib_tasa"].ToString().ToString()),
                                          double.Parse(mainTable.Attributes["fib_iva"].ToString().ToString()), Int32.Parse(mainTable.Attributes["fib_periodos"].ToString().ToString()),
                                          dateInicio, bool.Parse(mainTable.Attributes["fib_pergracia"].ToString().ToString()),
                                          dateGracia, ((entityName == "fib_configamortiza") ? bool.Parse(mainTable.Attributes["fib_creditofinan"].ToString().ToString()) : false),
                                          Int32.Parse(mainTable.Contains("fib_numpergracia") ? mainTable.Attributes["fib_numpergracia"].ToString() : "0"));

                PConsole.writeLine("setTable 6");
                List<Periodo> Pagos = (calcpago.Attributes["fib_name"].ToString().ToUpper().Contains("ÚNICO")) ? TablaAmortiza.createTableUnPago() : TablaAmortiza.createTable();

                Periodo ultimopago = Pagos.Last(x => x.periodo == Int32.Parse(mainTable.Attributes["fib_periodos"].ToString()));
                this.ultpagofecha = ultimopago.fecha;
                List<Periodo> Seguro = new List<Periodo>();



                PConsole.writeLine("setTable 7");

                if (entityName == "fib_configamortiza") // Es para la entidad de Disposiciones
                {
                    if (!mainTable.Attributes.Contains("fib_ultfechadepago"))
                    {
                        mainTable.Attributes.Add("fib_ultfechadepago", this.ultpagofecha);
                    }
                    string message = "";

                    if (DateTime.Parse(mainTable.Attributes["fib_fechaviglncredito"].ToString()) <= DateTime.Parse(mainTable.Attributes["fib_ultfechadepago"].ToString()))
                    {
                        PConsole.writeLine("setTable 7.1");
                        message = "Error: El cálculo de la tabla de amortización proyecto que la fecha del ultimo pago  " + mainTable.Attributes["fib_ultfechadepago"].ToString() + " es mayor o igual a la fecha de vigencia de la línea de crédito " + mainTable.Attributes["fib_fechaviglncredito"].ToString();
                        message += "\n La duración de una disposición de crédito no puede ser mayor que la vigencia de la línea de crédito asociada a la disposción.";
                        result = message;
                    }
                    else
                    {
                        PConsole.writeLine("setTable 7.2");
                        if (mainTable.Attributes.Contains("fib_seguronofinanciado") && mainTable.Attributes.Contains("fib_periodosseguro"))
                        {
                            Entity EntidadProcesar = servicio.Retrieve(ProcesarId.LogicalName, ProcesarId.Id, new ColumnSet("fib_seguro"));
                            bool contieneTipoSeguro = EntidadProcesar.Attributes.Contains("fib_seguro_multianual");
                            int seguromultianual = (contieneTipoSeguro) ? EntidadProcesar.GetAttributeValue<OptionSetValue>("fib_seguro").Value : 0;

                            Money fib_importeseguro = (Money)mainTable.Attributes["fib_seguronofinanciado"];
                            double importeSeguro = double.Parse(fib_importeseguro.Value.ToString());
                            Int32 plazoSeguro = Int32.Parse(mainTable.Attributes["fib_periodosseguro"].ToString());

                            PConsole.writeLine(contieneTipoSeguro.ToString());

                            if (contieneTipoSeguro && seguromultianual == 4)
                            {
                                PConsole.writeLine("setTable 7.3: " + plazoSeguro.ToString());
                                PConsole.writeLine("setTable 7.4: " + fib_importeseguro.Value.ToString());
                                double iva = double.Parse(mainTable.Attributes["fib_iva"].ToString().ToString());
                                double tasaSeguro = Math.Round(importeSeguro / 12, 2);
                                foreach (var pago in Pagos)
                                    Seguro.Add(new Periodo
                                    {
                                        periodo = pago.periodo,
                                        fecha = pago.fecha,
                                        pago = tasaSeguro,
                                        interes = 0,
                                        capital = 0,
                                        iva = 0,
                                        freccapital = 0,
                                        saldofinal = 0,
                                        saldoInicial = 0,
                                        tsiniva = 0
                                    });
                            }
                            #region Si es seguro Multianual y Financiado
                            else if (infoSeguro.fib_seguro_multianual == 1 && infoSeguro.fib_tipo_seguro_multianual == 6)
                            {
                                //Money fib_importeseguro = (Money)mainTable.Attributes["fib_seguronofinanciado"];
                                //double importeSeguro = double.Parse(fib_importeseguro.Value.ToString());
                                //Money fib_importeseguro = (Money)mainTable.Attributes["fib_importeseguro"];
                                //double importeSeguro = double.Parse(fib_importeseguro.Value.ToString());

                                //double importeSeguro = double.Parse("0");
                                //Int32 plazoSeguro = Int32.Parse(mainTable.Attributes["fib_periodosseguro"].ToString());

                                GenAmortiza TablaAmortiza2 = new GenAmortiza();
                                /*TablaAmortiza2.configTable(periodo.fib_equivalenciaendias.Value, anualidad,
                                                           (double)mainTable.fib_seguronofinanciado.Value, (double)mainTable.fib_tasa.Value,
                                                            mainTable.fib_iva.Value,
                                                            mainTable.fib_periodosseguro.Value,
                                                            dateInicio, mainTable.fib_pergracia.Value,
                                                            dateGracia, false,
                                                            mainTable.fib_numpergracia.Value);*/
                                PConsole.writeLine("setTable 7.2.1");
                                TablaAmortiza2.configTable(Int32.Parse(periodo.Attributes["fib_equivalenciaendias"].ToString()), anualidad,
                                       importeSeguro, double.Parse(mainTable.Attributes["fib_tasa"].ToString()),
                                       double.Parse(mainTable.Attributes["fib_iva"].ToString()), plazoSeguro,
                                       dateInicio, bool.Parse(mainTable.Attributes["fib_pergracia"].ToString()),
                                       dateGracia, false, Int32.Parse(mainTable.Contains("fib_numpergracia") ? mainTable.Attributes["fib_numpergracia"].ToString() : "0"));
                                PConsole.writeLine("setTable 7.2.2");
                                Seguro = TablaAmortiza2.createTable();
                                PConsole.writeLine("setTable 7.2.3");
                            }
                            #endregion
                            else
                            {

                                GenAmortiza TablaAmortiza2 = new GenAmortiza();
                                /*TablaAmortiza2.configTable(periodo.fib_equivalenciaendias.Value, anualidad,
                                                           (double)mainTable.fib_seguronofinanciado.Value, (double)mainTable.fib_tasa.Value,
                                                            mainTable.fib_iva.Value,
                                                            mainTable.fib_periodosseguro.Value,
                                                            dateInicio, mainTable.fib_pergracia.Value,
                                                            dateGracia, false,
                                                            mainTable.fib_numpergracia.Value);*/
                                PConsole.writeLine("setTable 7.2.1");
                                TablaAmortiza2.configTable(Int32.Parse(periodo.Attributes["fib_equivalenciaendias"].ToString()), anualidad,
                                       importeSeguro, double.Parse(mainTable.Attributes["fib_tasa"].ToString()),
                                       double.Parse(mainTable.Attributes["fib_iva"].ToString()), plazoSeguro,
                                       dateInicio, bool.Parse(mainTable.Attributes["fib_pergracia"].ToString()),
                                       dateGracia, false, Int32.Parse(mainTable.Contains("fib_numpergracia") ? mainTable.Attributes["fib_numpergracia"].ToString() : "0"));
                                PConsole.writeLine("setTable 7.2.2");
                                Seguro = TablaAmortiza2.createTable();
                                PConsole.writeLine("setTable 7.2.3");
                            }

                        }
                        #region Para Seguro MultiAnual - Disposiciones

                        #region Si es Seguro Multianual y Financiado 1er año

                        else if (infoSeguro.fib_seguro_multianual == 1 && infoSeguro.fib_tipo_seguro_multianual == 5)
                        {
                            // Money fib_importeseguro = (Money)mainTable.Attributes["fib_seguronofinanciado"];
                            double importeSeguro = Convert.ToDouble(infoSeguro.fib_pago1_con_iva);
                            Int32 plazoSeguro = 12;

                            GenAmortiza TablaAmortiza2 = new GenAmortiza();
                            /*TablaAmortiza2.configTable(periodo.fib_equivalenciaendias.Value, anualidad,
                                                       (double)mainTable.fib_seguronofinanciado.Value, (double)mainTable.fib_tasa.Value,
                                                        mainTable.fib_iva.Value,
                                                        mainTable.fib_periodosseguro.Value,
                                                        dateInicio, mainTable.fib_pergracia.Value,
                                                        dateGracia, false,
                                                        mainTable.fib_numpergracia.Value);*/
                            PConsole.writeLine("setTable 7.2.1");
                            TablaAmortiza2.configTable(Int32.Parse(periodo.Attributes["fib_equivalenciaendias"].ToString()), anualidad,
                                   importeSeguro, double.Parse(mainTable.Attributes["fib_tasa"].ToString()),
                                   double.Parse(mainTable.Attributes["fib_iva"].ToString()), plazoSeguro,
                                   dateInicio, bool.Parse(mainTable.Attributes["fib_pergracia"].ToString()),
                                   dateGracia, false, Int32.Parse(mainTable.Contains("fib_numpergracia") ? mainTable.Attributes["fib_numpergracia"].ToString() : "0"));
                            PConsole.writeLine("setTable 7.2.2");
                            Seguro = TablaAmortiza2.createTable();
                            PConsole.writeLine("setTable 7.2.3");
                        }

                        #endregion

                        #region Si es seguro Multianual y Financiado
                        else if (infoSeguro.fib_seguro_multianual == 1 && infoSeguro.fib_tipo_seguro_multianual == 6)
                        {
                            Money fib_importeseguro = (Money)mainTable.Attributes["fib_seguronofinanciado"];
                            double importeSeguro = double.Parse(fib_importeseguro.Value.ToString());
                            //Money fib_importeseguro = (Money)mainTable.Attributes["fib_importeseguro"];
                            //double importeSeguro = double.Parse(fib_importeseguro.Value.ToString());

                            //double importeSeguro = double.Parse("0");
                            Int32 plazoSeguro = Int32.Parse(mainTable.Attributes["fib_periodosseguro"].ToString());

                            GenAmortiza TablaAmortiza2 = new GenAmortiza();
                            /*TablaAmortiza2.configTable(periodo.fib_equivalenciaendias.Value, anualidad,
                                                       (double)mainTable.fib_seguronofinanciado.Value, (double)mainTable.fib_tasa.Value,
                                                        mainTable.fib_iva.Value,
                                                        mainTable.fib_periodosseguro.Value,
                                                        dateInicio, mainTable.fib_pergracia.Value,
                                                        dateGracia, false,
                                                        mainTable.fib_numpergracia.Value);*/
                            PConsole.writeLine("setTable 7.2.1");
                            TablaAmortiza2.configTable(Int32.Parse(periodo.Attributes["fib_equivalenciaendias"].ToString()), anualidad,
                                   importeSeguro, double.Parse(mainTable.Attributes["fib_tasa"].ToString()),
                                   double.Parse(mainTable.Attributes["fib_iva"].ToString()), plazoSeguro,
                                   dateInicio, bool.Parse(mainTable.Attributes["fib_pergracia"].ToString()),
                                   dateGracia, false, Int32.Parse(mainTable.Contains("fib_numpergracia") ? mainTable.Attributes["fib_numpergracia"].ToString() : "0"));
                            PConsole.writeLine("setTable 7.2.2");
                            Seguro = TablaAmortiza2.createTable();
                            PConsole.writeLine("setTable 7.2.3");
                        }
                        #endregion

                        #endregion

                        //mainTable.fib_frecuenciadecapital = new SdkWs.CrmDecimal();
                        //mainTable.fib_frecuenciadecapital.Value = (decimal)TablaAmortiza.freccinipago;
                        //genAmortizaTable(servicio, Pagos, Seguro, mainTable);

                    }
                }
                else // Es para la entidad de Cotizaciones
                {


                    if (mainTable.Attributes.Contains("fib_importeseguro") && mainTable.Attributes.Contains("fib_plazoseguro"))
                    {
                        Entity EntidadProcesar = servicio.Retrieve(ProcesarId.LogicalName, ProcesarId.Id, new ColumnSet("fib_seguro_multianual"));
                        bool contieneCheck = EntidadProcesar.Attributes.Contains("fib_seguro_multianual");
                        bool seguromultianual = (contieneCheck) ? bool.Parse(EntidadProcesar.Attributes["fib_seguro_multianual"].ToString()) : false;

                        Money fib_importeseguro = (Money)mainTable.Attributes["fib_importeseguro"];
                        double importeSeguro = double.Parse(fib_importeseguro.Value.ToString());
                        Int32 plazoSeguro = Int32.Parse(mainTable.Attributes["fib_plazoseguro"].ToString());

                        PConsole.writeLine(contieneCheck.ToString());
                        //PConsole.writeLine(EntidadProcesar.Attributes["fib_seguro_multianual"].ToString());

                        #region Para Seguro Multianual - Cotizaciones

                        #region Seguro Multianual y Pago Anual

                        //Seguro multianual y Pago Anual
                        if (infoSeguro.fib_seguro_multianual == 1 && infoSeguro.fib_tipo_seguro_multianual == 1)
                        {
                            PConsole.writeLine("setTable 7.3: " + plazoSeguro.ToString());
                            PConsole.writeLine("setTable 7.4: " + fib_importeseguro.Value.ToString());
                            double iva = double.Parse(mainTable.Attributes["fib_iva"].ToString().ToString());
                            double tasaSeguro = Math.Round(importeSeguro / plazoSeguro, 2);
                            foreach (var pago in Pagos)
                                Seguro.Add(new Periodo
                                {
                                    periodo = pago.periodo,
                                    fecha = pago.fecha,
                                    pago = 0,//tasaSeguro,
                                    interes = 0,
                                    capital = 0,
                                    iva = 0,
                                    freccapital = 0,
                                    saldofinal = 0,
                                    saldoInicial = 0,
                                    tsiniva = 0
                                });
                        }

                        #endregion

                        #region Seguro Multianusl y Pago anual con primer año financiado
                        else if (infoSeguro.fib_seguro_multianual == 1 && infoSeguro.fib_tipo_seguro_multianual == 2) // Seguro Multianusl y Pago anual con primer año financiado
                        {
                            PConsole.writeLine("setTable 7.5: " + plazoSeguro.ToString());
                            PConsole.writeLine("setTable 7.6: " + fib_importeseguro.Value.ToString());

                            GenAmortiza TablaAmortiza2 = new GenAmortiza();
                            plazoSeguro = 12;//1er año financiado
                            importeSeguro = Convert.ToDouble(infoSeguro.fib_pago1_con_iva);

                            TablaAmortiza2.configTable(Int32.Parse(periodo.Attributes["fib_equivalenciaendias"].ToString()), anualidad,
                                       importeSeguro, double.Parse(mainTable.Attributes["fib_tasa"].ToString()),
                                       double.Parse(mainTable.Attributes["fib_iva"].ToString()), plazoSeguro,
                                       dateInicio, bool.Parse(mainTable.Attributes["fib_pergracia"].ToString()),
                                       dateGracia, false, Int32.Parse(mainTable.Contains("fib_numpergracia") ? mainTable.Attributes["fib_numpergracia"].ToString() : "0"));


                            Seguro = TablaAmortiza2.createTable();


                        }
                        #endregion

                        #region Seguro Multianusl y Financiado
                        else if (infoSeguro.fib_seguro_multianual == 1 && infoSeguro.fib_tipo_seguro_multianual == 3) // Seguro Multianusl y Pago anual con primer año financiado
                        {
                            PConsole.writeLine("setTable 7.5: " + plazoSeguro.ToString());
                            PConsole.writeLine("setTable 7.6: " + fib_importeseguro.Value.ToString());

                            GenAmortiza TablaAmortiza2 = new GenAmortiza();
                            //plazoSeguro = plazoSeguro;//1er año financiado
                            //importeSeguro = Convert.ToDouble(infoSeguro.fib_pago1_con_iva);

                            TablaAmortiza2.configTable(Int32.Parse(periodo.Attributes["fib_equivalenciaendias"].ToString()), anualidad,
                                       importeSeguro, double.Parse(mainTable.Attributes["fib_tasa"].ToString()),
                                       double.Parse(mainTable.Attributes["fib_iva"].ToString()), plazoSeguro,
                                       dateInicio, bool.Parse(mainTable.Attributes["fib_pergracia"].ToString()),
                                       dateGracia, false, Int32.Parse(mainTable.Contains("fib_numpergracia") ? mainTable.Attributes["fib_numpergracia"].ToString() : "0"));


                            Seguro = TablaAmortiza2.createTable();
                        }


                        #endregion

                        #endregion

                        else if (infoSeguro.fib_seguro_multianual == 0)//Si no esta seleccionado pago multianual
                        {
                            PConsole.writeLine("setTable 7.5: " + plazoSeguro.ToString());
                            PConsole.writeLine("setTable 7.6: " + fib_importeseguro.Value.ToString());

                            GenAmortiza TablaAmortiza2 = new GenAmortiza();
                            TablaAmortiza2.configTable(Int32.Parse(periodo.Attributes["fib_equivalenciaendias"].ToString()), anualidad,
                                       importeSeguro, double.Parse(mainTable.Attributes["fib_tasa"].ToString()),
                                       double.Parse(mainTable.Attributes["fib_iva"].ToString()), plazoSeguro,
                                       dateInicio, bool.Parse(mainTable.Attributes["fib_pergracia"].ToString()),
                                       dateGracia, false, Int32.Parse(mainTable.Contains("fib_numpergracia") ? mainTable.Attributes["fib_numpergracia"].ToString() : "0"));

                            Seguro = TablaAmortiza2.createTable();
                            //PConsole.writeLine("setTable 7.3: " + plazoSeguro.ToString());
                            //PConsole.writeLine("setTable 7.4: " + fib_importeseguro.Value.ToString());
                            //double iva = double.Parse(mainTable.Attributes["fib_iva"].ToString().ToString());
                            //double tasaSeguro = Math.Round(importeSeguro / plazoSeguro, 2);
                            //foreach (var pago in Pagos)
                            //    Seguro.Add(new Periodo
                            //    {
                            //        periodo = pago.periodo,
                            //        fecha = pago.fecha,
                            //        pago = tasaSeguro,
                            //        interes = 0,
                            //        capital = 0,
                            //        iva = 0,
                            //        freccapital = 0,
                            //        saldofinal = 0,
                            //        saldoInicial = 0,
                            //        tsiniva = 0
                            //    });
                        }



                        //if (contieneCheck && seguromultianual)
                        //{
                        //    PConsole.writeLine("setTable 7.3: " + plazoSeguro.ToString());
                        //    PConsole.writeLine("setTable 7.4: " + fib_importeseguro.Value.ToString());
                        //    double iva = double.Parse(mainTable.Attributes["fib_iva"].ToString().ToString());
                        //    double tasaSeguro = Math.Round(importeSeguro / plazoSeguro, 2);
                        //    foreach (var pago in Pagos)
                        //        Seguro.Add(new Periodo
                        //        {
                        //            periodo = pago.periodo,
                        //            fecha = pago.fecha,
                        //            pago = tasaSeguro,
                        //            interes = 0,
                        //            capital = 0,
                        //            iva = 0,
                        //            freccapital = 0,
                        //            saldofinal = 0,
                        //            saldoInicial = 0,
                        //            tsiniva = 0
                        //        });
                        //}
                        //else
                        //{
                        //    PConsole.writeLine("setTable 7.5: " + plazoSeguro.ToString());
                        //    PConsole.writeLine("setTable 7.6: " + fib_importeseguro.Value.ToString());

                        //    GenAmortiza TablaAmortiza2 = new GenAmortiza();
                        //    TablaAmortiza2.configTable(Int32.Parse(periodo.Attributes["fib_equivalenciaendias"].ToString()), anualidad,
                        //               importeSeguro, double.Parse(mainTable.Attributes["fib_tasa"].ToString()),
                        //               double.Parse(mainTable.Attributes["fib_iva"].ToString()), plazoSeguro,
                        //               dateInicio, bool.Parse(mainTable.Attributes["fib_pergracia"].ToString()),
                        //               dateGracia, false, Int32.Parse(mainTable.Contains("fib_numpergracia") ? mainTable.Attributes["fib_numpergracia"].ToString() : "0"));

                        //     Seguro = TablaAmortiza2.createTable();

                        //}
                    }
                }


                if (!mainTable.Attributes.Contains("fib_frecuenciadecapital"))
                {
                    mainTable.Attributes.Add("fib_frecuenciadecapital", new Decimal(TablaAmortiza.freccinipago));
                }
                PConsole.writeLine("setTable 8");
                genAmortizaTable(servicio, Pagos, Seguro, mainTable, entityName);

                if (!mainTable.Attributes.Contains("fib_resultado"))
                {
                    mainTable.Attributes.Add("fib_resultado", result);
                }

                PConsole.writeLine("fib_resultado: " + result.ToString());
                mainTable.EntityState = null;
                servicio.Update(mainTable);
                PConsole.writeLine("setTable FIN");
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                PConsole.writeLine("ERROR 1: " + ex.Message);
                throw new InvalidPluginExecutionException("Error al actualizar: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                PConsole.writeLine("ERROR 2: " + ex.Message);
                throw new InvalidPluginExecutionException("Error: " + ex.Message);
            }
        }

        private Entity getConfigTabla(IOrganizationService servicio, Guid idta)
        {
            try
            {
                ColumnSet cols = new ColumnSet();
                cols.AllColumns = true;
                Entity cfgamortiza = (Entity)servicio.Retrieve("fib_configamortiza2", idta, cols);
                PConsole.writeLine("getConfigTabla 0");
                return cfgamortiza;
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                throw new InvalidPluginExecutionException("Error_getConfigTabla: ", ex);
            }
        }

        private void clearOldConfigs(IOrganizationService servicio, Guid idcotizacion, Guid idta, string entityName)
        {
            PConsole.writeLine("clearOldConfigs 0");
            ConditionExpression condition = new ConditionExpression();
            //condition.AttributeName = "fib_cotizacionid";
            condition.AttributeName = ((entityName == "fib_configamortiza") ? "fib_disposicioncreditoid" : "fib_cotizacionid");
            condition.Operator = ConditionOperator.Equal;
            condition.Values.Add(idcotizacion);

            ConditionExpression condition2 = new ConditionExpression();
            //condition2.AttributeName = "fib_configamortiza2id";
            condition2.AttributeName = ((entityName == "fib_configamortiza") ? "fib_configamortizaid" : "fib_configamortiza2id");
            condition2.Operator = ConditionOperator.NotEqual;
            condition2.Values.Add(idta);

            FilterExpression filter = new FilterExpression();
            filter.FilterOperator = LogicalOperator.And;
            filter.Conditions.AddRange(new ConditionExpression[] { condition, condition2 });

            QueryExpression query = new QueryExpression();
            query.EntityName = ((entityName == "fib_configamortiza") ? "fib_configamortiza" : "fib_configamortiza2");
            query.ColumnSet = new ColumnSet(true);
            query.Criteria = filter;

            try
            {
                PConsole.writeLine("clearOldConfigs 1");
                EntityCollection configs = servicio.RetrieveMultiple(query);
                //Entity configta = new Entity("fib_configamortiza2");

                foreach (Entity entity in configs.Entities)
                {
                    //configta = entity;
                    //servicio.Delete("fib_configamortiza2", new Guid(entity.Attributes["fib_configamortiza2id"].ToString()));
                    servicio.Delete(entityName, new Guid(entity.Attributes[((entityName == "fib_configamortiza") ? "fib_configamortizaid" : "fib_configamortiza2id")].ToString()));
                }

                PConsole.writeLine("clearOldConfigs FIN");
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                throw new InvalidPluginExecutionException("Error_clearOldConfigs: ", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error_clearOldConfigs: " + ex.Message);
            }
        }

        private void clearOldsPeriodos(IOrganizationService servicio, Guid idcotizacion, string entityName)
        {
            PConsole.writeLine("clearOldsPeriodos 0");
            ConditionExpression condition = new ConditionExpression();
            //condition.AttributeName = "fib_tablaamortizacinid";
            condition.AttributeName = ((entityName == "fib_configamortiza") ? "fib_creditoid" : "fib_tablaamortizacinid");
            condition.Operator = ConditionOperator.Equal;
            condition.Values.Add(idcotizacion);

            FilterExpression filter = new FilterExpression();
            filter.FilterOperator = LogicalOperator.And;
            filter.Conditions.Add(condition);

            QueryExpression query = new QueryExpression();
            query.EntityName = "fib_amortizacion";
            query.ColumnSet = new ColumnSet(true);
            query.Criteria = filter;

            try
            {
                EntityCollection pagos = servicio.RetrieveMultiple(query);
                //Entity pago = new Entity("fib_amortizacion");
                foreach (Entity entity in pagos.Entities)
                {
                    //pago = entity;
                    servicio.Delete("fib_amortizacion", new Guid(entity.Attributes["fib_amortizacionid"].ToString()));
                }
                PConsole.writeLine("clearOldsPeriodos FIN");
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                throw new InvalidPluginExecutionException("Error: ", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error: " + ex.Message);
            }
        }

        private Entity getPeriodo(IOrganizationService servicio, Guid idper)
        {
            try
            {
                Entity periodicidad = new Entity("fib_catalogodeperiododepago");
                periodicidad = servicio.Retrieve("fib_catalogodeperiododepago", idper, new ColumnSet(true));
                PConsole.writeLine("getPeriodo FIN");
                return periodicidad;
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                throw new InvalidPluginExecutionException("Error: ", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error: " + ex.Message);
            }
        }

        private Entity getCalcPago(IOrganizationService servicio, Guid idcalc)
        {
            try
            {
                Entity caclpago = servicio.Retrieve("fib_catalogodecalculodepago", idcalc, new ColumnSet(true));
                PConsole.writeLine("getCalcPago FIN");
                return caclpago;
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                throw new InvalidPluginExecutionException("Error: ", ex);
            }
        }

        private void genAmortizaTable(IOrganizationService servicio, List<Periodo> Pagos, List<Periodo> Seguro, Entity mainTable, string entityName)
        {
            try
            {
                PConsole.writeLine("genAmortizaTable 0");
                double CatPre = 0;
                double CatValor = 0;
                double CatFinal = 0;
                bool Primero = true;
                List<double> ValoresIIR = new List<double>();

                //bool segAmortizado = (mainTable.Attributes["fib_importeseguro"] != null && mainTable.Attributes["fib_plazoseguro"] != null);
                //bool segAmortizado = (mainTable.GetAttributeValue<Money>("fib_importeseguro").Value != null && mainTable.GetAttributeValue<int>("fib_plazoseguro") != null);
                // RGomezS - 15/02/2016
                //bool segAmortizado = (mainTable.Contains("fib_importeseguro") && mainTable.Contains("fib_plazoseguro"));
                bool segAmortizado = (mainTable.Attributes.Contains("fib_seguronofinanciado") && mainTable.Attributes.Contains("fib_periodosseguro")) || (mainTable.Attributes.Contains("fib_importeseguro") && mainTable.Attributes.Contains("fib_plazoseguro"));
                bool PagosProg = false;
                // RGomezS - 15/02/2016

                PConsole.writeLine("segAmortizado: " + segAmortizado.ToString());

                if (bool.Parse(mainTable.Attributes["fib_pergracia"].ToString()) && !bool.Parse(mainTable.Attributes["fib_periodocero"].ToString()) && Int32.Parse(mainTable.Attributes["fib_numpergracia"].ToString()) < 2)
                {
                    PConsole.writeLine("genAmortizaTable 1");
                    Periodo percero = Pagos.Find(p => p.periodo == 0);    // obtenemos el periodo cero
                    int index = Pagos.FindIndex(p => p.periodo == 1);     // obtenemos la posición del periodo 1
                    Pagos[index].interes += percero.interes;              // le sumamos el interes y el pago del periodo cero
                    Pagos[index].iva += percero.iva;
                    Pagos[index].pago += percero.pago;
                    Pagos[index].freccapital += percero.freccapital;
                    Pagos.Remove(percero);                                // quitamos el periodo cero;

                    // hacemos lo mismo con la amortización del seguro si es que hay
                    if (segAmortizado)
                    {
                        Periodo perceroseg = Seguro.Find(p => p.periodo == 0);
                        index = Seguro.FindIndex(p => p.periodo == 1);
                        Seguro[index].interes += perceroseg.interes;
                        Seguro[index].pago += perceroseg.pago;
                        Seguro[index].iva += perceroseg.iva;
                        Seguro.Remove(perceroseg);
                    }
                }

                PConsole.writeLine("genAmortizaTable 2");

                //if (mainTable.Attributes["fib_pagosprogramados"] != null)
                if (mainTable.Attributes.Contains("fib_pagosprogramados"))
                {
                    PagosGen = new List<Entity>();
                    PagosProg = true;
                    PConsole.writeLine("genAmortizaTable 2.1: " + PagosProg.ToString());
                }
                var tabAmor = "";

                // RGomezS - 15/02/2016
                PConsole.writeLine("Codigo sin uso");
                // No se usa y la división podria dar error de division entre 0
                //double seguroNoFinan = (mainTable.Attributes.Contains("fib_seguronofinanciado")) ? double.Parse(mainTable.Attributes["fib_seguronofinanciado"].ToString()) : 0;
                //int perseg = (mainTable.Attributes.Contains("fib_periodosseguro")) ? int.Parse(mainTable.Attributes["fib_periodosseguro"].ToString()) : 0;
                //double importeSeg = (perseg != 0) ? seguroNoFinan / perseg : 0;
                //importeSeg = (double.IsNaN(importeSeg)) ? 0 : importeSeg;
                // RGomezS - 15/02/2016


                #region Nuevo para Seguro Multianual

                ///Se consulta información del seguro
                ///dependiendo si es una Disposición o Cotización
                Seguro confSeguro = new Seguro();
                confSeguro = getTiposeguro(mainTable);

                #endregion

                int cont = 1;
                PConsole.writeLine("Pagos: " + Pagos.Count.ToString());
                foreach (Periodo pago in Pagos)
                {

                    decimal ComisionAmdon = 0;
                    if (entityName == "fib_configamortiza2") //Si es cotización se calcula comisión
                    {
                        //Para calcular la comisión por Administración
                        ComisionAmdon = (confSeguro.fib_aplica_comision_administracion == 1) ? (1 + (confSeguro.fib_iva_comision_administracion / 100)) * confSeguro.fib_comision_administracion_sin_iva : 0;
                    }


                    tabAmor += pago.periodo.ToString() + " " + (pago.capital + pago.interes).ToString() + "    ";
                    Entity amortiza = new Entity("fib_amortizacion");

                    amortiza.Attributes.Add("fib_periodo", pago.periodo);

                    amortiza.Attributes.Add("fib_aceptapagoprogramado", (pgirregular && !segAmortizado));

                    amortiza.Attributes.Add("fib_fecha", pago.fecha);
                    PConsole.writeLine("fib_saldoinicial " + pago.saldoInicial.ToString());
                    amortiza.Attributes.Add("fib_saldoinicial", new Money((decimal)pago.saldoInicial));
                    PConsole.writeLine("fib_capital " + pago.capital.ToString());
                    amortiza.Attributes.Add("fib_capital", new Money((decimal)pago.capital));
                    PConsole.writeLine("fib_interes " + pago.interes.ToString());
                    amortiza.Attributes.Add("fib_interes", new Money((decimal)pago.interes));
                    PConsole.writeLine("fib_iva " + pago.iva.ToString());
                    amortiza.Attributes.Add("fib_iva", pago.iva);

                    /*if (entityName == "fib_configamortiza")
                        amortiza.Attributes.Add("fib_periodo", pago.periodo);*/
                    PConsole.writeLine("fib_pago");
                    amortiza.Attributes.Add("fib_pago", new Money((decimal)pago.pago + ComisionAmdon));
                    PConsole.writeLine("fib_saldofinal");
                    amortiza.Attributes.Add("fib_saldofinal", new Money((decimal)pago.saldofinal));
                    PConsole.writeLine("fib_diasdelperiodo");
                    amortiza.Attributes.Add("fib_diasdelperiodo", (decimal)pago.freccapital);
                    PConsole.writeLine("fib_tasasinivaperiodo");
                    amortiza.Attributes.Add("fib_tasasinivaperiodo", (decimal)pago.tsiniva);

                    //EntityReference fib_tablaamortizacinid = (EntityReference)mainTable.Attributes["fib_cotizacionid"];
                    //Aqui se condiciona dependiendo si es una DISPOSICION o una COTIZACION.
                    EntityReference fib_tablaamortizacinid = (EntityReference)mainTable.Attributes[((entityName == "fib_configamortiza") ? "fib_disposicioncreditoid" : "fib_cotizacionid")];
                    PConsole.writeLine("genAmortizaTable 3: " + cont + " - " + fib_tablaamortizacinid.Id);
                    amortiza.Attributes.Add(((entityName == "fib_configamortiza") ? "fib_creditoid" : "fib_tablaamortizacinid"), new EntityReference(((entityName == "fib_configamortiza") ? "fib_credito" : "fib_cotizador"), fib_tablaamortizacinid.Id));

                    amortiza.Attributes.Add("fib_segnofinanciado", new Money(0));

                    amortiza.Attributes.Add("fib_pagomasseguro", new Money(0));

                    amortiza.Attributes.Add("fib_pagoprogramado", new Money(0));

                    amortiza.Attributes["fib_pagomasseguro"] = new Money((decimal)pago.pago + ComisionAmdon);

                    if (Seguro.Exists(x => x.periodo.Equals(pago.periodo)))
                    {
                        Periodo pgSeg0 = Seguro.Find(x => x.periodo.Equals(pago.periodo));
                        amortiza.Attributes["fib_pagomasseguro"] = new Money((decimal)pago.pago + (decimal)pgSeg0.pago + ComisionAmdon);
                    }


                    // Si Tiene seguro financiado sin pagos programados, se suma de una vez el seguro financiado
                    if (segAmortizado && Seguro.Exists(x => x.periodo.Equals(pago.periodo)) && !PagosProg)
                    {
                        PConsole.writeLine("genAmortizaTable 4: " + cont);
                        Periodo pgSeg = Seguro.Find(x => x.periodo.Equals(pago.periodo));
                        amortiza.Attributes["fib_saldoinicial"] = new Money(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_saldoinicial").Value.ToString()) + (decimal)pgSeg.saldoInicial);

                        amortiza.Attributes["fib_capital"] = new Money(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_capital").Value.ToString()) + (decimal)pgSeg.capital);

                        amortiza.Attributes["fib_interes"] = new Money(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_interes").Value.ToString()) + (decimal)pgSeg.interes);

                        amortiza.Attributes["fib_iva"] = double.Parse(amortiza.Attributes["fib_iva"].ToString()) + pgSeg.iva;

                        amortiza.Attributes["fib_pago"] = new Money((decimal)pago.pago + (decimal)pgSeg.pago + ComisionAmdon);

                        amortiza.Attributes["fib_saldofinal"] = new Money(Math.Round(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_saldoinicial").Value.ToString()) - decimal.Parse(amortiza.GetAttributeValue<Money>("fib_capital").Value.ToString()), 2));

                        amortiza.Attributes["fib_segnofinanciado"] = new Money((decimal)pgSeg.pago);

                        amortiza.Attributes["fib_pagomasseguro"] = new Money((decimal)pago.pago + (decimal)pgSeg.pago + ComisionAmdon);// new Money(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_pago").Value.ToString()));
                        PConsole.writeLine("genAmortizaTable 5: " + cont);

                    }

                    #region Ajustes para Seguro Multianual

                    #region Seguro Multianual - Pago Anual

                    if (confSeguro.fib_seguro_multianual == 1 && //Si esta marcado seguro multianual 
                                            ((entityName == "fib_configamortiza2" && confSeguro.fib_tipo_seguro_multianual == 1)  // Si es Cotiazación y es Pago Anual 
                                            || (entityName == "fib_configamortiza" && confSeguro.fib_tipo_seguro_multianual == 4))// Si es Disposicion y es Pago Anual
                                            )
                    {

                        if (pago.periodo == 1) //Si es el primer pago, se asigna importe del campo “Pago1 Con IVA” de entidad Cotizaciones
                        {

                            amortiza.Attributes["fib_segnofinanciado"] = new Money(0); //new Money(confSeguro.fib_pago1_con_iva);
                            amortiza.Attributes["fib_pagomasseguro"] = new Money((decimal)pago.pago + ComisionAmdon);//new Money(confSeguro.fib_pago1_con_iva + (decimal)pago.pago + ComisionAmdon);
                            amortiza.Attributes["fib_pago"] = new Money((decimal)pago.pago + ComisionAmdon);//new Money((decimal)pago.pago + (decimal)confSeguro.fib_pago1_con_iva + ComisionAmdon);


                        } if ((pago.periodo - 1) % 12 == 0 && pago.periodo > 1) //Si son pagos subsecuentes, se valida que el periodo calculado sea el mes 12 + 1, ejem: periodo 13,
                        {

                            amortiza.Attributes["fib_segnofinanciado"] = new Money(confSeguro.fib_pago_subsecuente_con_iva);
                            amortiza.Attributes["fib_pagomasseguro"] = new Money((decimal)pago.pago + (decimal)confSeguro.fib_pago_subsecuente_con_iva + ComisionAmdon);
                            amortiza.Attributes["fib_pago"] = new Money((decimal)pago.pago + (decimal)confSeguro.fib_pago_subsecuente_con_iva + ComisionAmdon);
                        }
                        else if ((pago.periodo - 1) % 12 != 0 && pago.periodo > 1)
                        {
                            amortiza.Attributes["fib_pago"] = new Money((decimal)pago.pago + ComisionAmdon);
                            amortiza.Attributes["fib_pagomasseguro"] = new Money((decimal)pago.pago + ComisionAmdon);
                        }

                    }

                    #endregion

                    #region Seguro Multianual - Pago Anual 1er año financiado


                    else if (confSeguro.fib_seguro_multianual == 1 && //Si esta marcado Seguro Multianual 
                                ((entityName == "fib_configamortiza2" && confSeguro.fib_tipo_seguro_multianual == 2) //En Cotización es Pago Anual 1er año financiado 
                                  || (entityName == "fib_configamortiza" && confSeguro.fib_tipo_seguro_multianual == 5) //En Disposición es Pago Anual 1er año financiado
                                )
                             )
                    {
                        Periodo pgSeg = Seguro.Find(x => x.periodo.Equals(pago.periodo));

                        //Para disposición, calcular Saldo Inicial, Saldo Final
                        if (entityName == "fib_configamortiza" && Seguro.Exists(x => x.periodo.Equals(pago.periodo)))
                        {
                            amortiza.Attributes["fib_saldoinicial"] = new Money(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_saldoinicial").Value.ToString()) + (decimal)pgSeg.saldoInicial);
                            amortiza.Attributes["fib_capital"] = new Money(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_capital").Value.ToString()) + (decimal)pgSeg.capital);

                            amortiza.Attributes["fib_interes"] = new Money(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_interes").Value.ToString()) + (decimal)pgSeg.interes);

                            amortiza.Attributes["fib_iva"] = double.Parse(amortiza.Attributes["fib_iva"].ToString()) + pgSeg.iva;

                            amortiza.Attributes["fib_pago"] = new Money((decimal)pago.pago + (decimal)pgSeg.pago + ComisionAmdon);

                            amortiza.Attributes["fib_saldofinal"] = new Money(Math.Round(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_saldoinicial").Value.ToString()) - decimal.Parse(amortiza.GetAttributeValue<Money>("fib_capital").Value.ToString()), 2));

                            amortiza.Attributes["fib_segnofinanciado"] = new Money((decimal)pgSeg.pago);

                            amortiza.Attributes["fib_pagomasseguro"] = new Money((decimal)pago.pago + (decimal)pgSeg.pago + ComisionAmdon);// new Money(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_pago").Value.ToString()));
                            //amortiza.Attributes["fib_saldofinal"] = new Money(Math.Round(decimal.Parse(amortiza.GetAttributeValue<Money>("fib_saldoinicial").Value.ToString()) - decimal.Parse(amortiza.GetAttributeValue<Money>("fib_capital").Value.ToString()), 2));
                        }


                        if ((pago.periodo - 1) % 12 == 0 && pago.periodo > 1) //Si son pagos subsecuentes, se valida que el periodo calculado sea el mes 12 + 1, ejem: periodo 13,
                        {
                            amortiza.Attributes["fib_segnofinanciado"] = new Money(confSeguro.fib_pago_subsecuente_con_iva);
                            amortiza.Attributes["fib_pagomasseguro"] = new Money(confSeguro.fib_pago_subsecuente_con_iva + (decimal)pago.pago + ComisionAmdon);
                            amortiza.Attributes["fib_pago"] = new Money((decimal)pago.pago + (decimal)confSeguro.fib_pago_subsecuente_con_iva + ComisionAmdon);
                        }
                        else
                        {
                            amortiza.Attributes["fib_segnofinanciado"] = new Money((pago.periodo <= 12) ? (decimal)pgSeg.pago : 0);
                            amortiza.Attributes["fib_pagomasseguro"] = new Money((decimal)pago.pago + ComisionAmdon + ((pago.periodo <= 12) ? (decimal)pgSeg.pago : 0));
                            amortiza.Attributes["fib_pago"] = new Money((decimal)pago.pago + ((pago.periodo <= 12) ? (decimal)pgSeg.pago : 0) + ComisionAmdon);
                        }

                    }

                    #endregion

                    #region Seguro Multianual - Financiado

                    else if (confSeguro.fib_seguro_multianual == 1 && //Si esta marcado seguro multianual 
                                (entityName == "fib_configamortiza2" && confSeguro.fib_tipo_seguro_multianual == 3) //En Cotización es Multianual Financiado
                                   || (entityName == "fib_configamortiza" && confSeguro.fib_tipo_seguro_multianual == 6)//En Disposición es Multianual Financiado
                                )
                    {
                        Periodo pgSeg = Seguro.Find(x => x.periodo.Equals(pago.periodo));
                        amortiza.Attributes["fib_segnofinanciado"] = new Money((decimal)pgSeg.pago);
                        amortiza.Attributes["fib_pagomasseguro"] = new Money((decimal)pago.pago + ComisionAmdon + (decimal)pgSeg.pago);
                        amortiza.Attributes["fib_pago"] = new Money((decimal)pago.pago + (decimal)pgSeg.pago + ComisionAmdon);
                    }

                    #endregion

                    #endregion

                    PConsole.writeLine("genAmortizaTable 6: " + cont);
                    Guid IdPer = servicio.Create(amortiza);

                    if (PagosProg)
                    {
                        PConsole.writeLine("genAmortizaTable 6.1: " + IdPer);
                        //amortiza.Attributes.Add("fib_amortizacionid", new EntityReference("fib_amortizacion", IdPer));
                        amortiza.Attributes.Add("fib_amortizacionid", IdPer);

                        PagosGen.Add(amortiza);
                    }
                    PConsole.writeLine("genAmortizaTable 7: " + IdPer.ToString());
                    cont++;
                }

                if (PagosProg)
                {
                    decimal ComisionAmdon = 0;
                    if (entityName == "fib_configamortiza2") //Si es cotización se calcula comisión
                    {
                        //Para calcular la comisión por Administración
                        ComisionAmdon = (confSeguro.fib_aplica_comision_administracion == 1) ? (1 + (confSeguro.fib_iva_comision_administracion / 100)) * confSeguro.fib_comision_administracion_sin_iva : 0;
                    }
                    setPagosProg(servicio, mainTable, segAmortizado, Seguro, ComisionAmdon);
                }
                //AQUI FINALIZA LA EJECUCIÓN DE DISPOSICION Y CONTINÚA LA DEL COTIZADOR

                if (entityName == "fib_configamortiza2")
                {
                    EntityReference fib_cotizacion = (EntityReference)mainTable.Attributes["fib_cotizacionid"];

                    PConsole.writeLine("genAmortizaTable 7: " + fib_cotizacion.Id);
                    EntityCollection PagosRecuperados = RecuperaPagos(servicio, fib_cotizacion.Id);

                    // Por cada Pago recuperado se construye la Lista que será enviada a la función del IRR
                    string vals = "";

                    cont = 0;
                    foreach (Entity Amortizacion in PagosRecuperados.Entities)
                    {
                        PConsole.writeLine("genAmortizaTable 8: " + cont);
                        decimal PTP = 0;
                        // Se guarda el primer valor del saldo Inicial
                        if (Primero)
                        {
                            Money fib_saldoinicial = (Money)Amortizacion.Attributes["fib_saldoinicial"];
                            ValoresIIR.Add(-Convert.ToDouble(fib_saldoinicial.Value));
                            vals += "Inicial " + fib_saldoinicial.Value.ToString() + "    ";
                        }
                        // Se suma lo que se especificó en el documento de diseño para ser tomado por el CAT
                        /*JER 12/Junio/2013 Se quita el Importe del seguro no Financiado. Así lo especificó Finbe*/
                        PConsole.writeLine("genAmortizaTable 8 FUERA IF: " + cont);
                        Money fib_capital = (Money)Amortizacion.Attributes["fib_capital"];
                        Money fib_interes = (Money)Amortizacion.Attributes["fib_interes"];
                        PTP = fib_capital.Value + fib_interes.Value; //+ Amortizacion.fib_segnofinanciado.Value;
                        // Se agrega dicho valor a la Lista del IRR
                        ValoresIIR.Add(Convert.ToDouble(PTP));
                        // Se cambia la bandera para no guardar Saldo Inicial
                        Primero = false;
                        Int32 fib_primero = (Int32)Amortizacion.Attributes["fib_periodo"];
                        vals += fib_primero.ToString() + " C=" + fib_capital.Value.ToString() + " I=" + fib_interes.Value.ToString() + " P=" + PTP.ToString() + "    ";

                        cont++;
                    }
                    PConsole.writeLine("genAmortizaTable 9");
                    // Hacemos uso de la función IRR para el calculo del CAT
                    CatPre = Financial.Irr(ValoresIIR, 0);
                    CatValor = Math.Pow((1 + CatPre), 12) - 1;
                    CatFinal = Math.Round(CatValor, 4) * 100;
                    PConsole.writeLine("genAmortizaTable 10");
                    PConsole.writeLine("CatPre=" + CatPre.ToString() + " CatValor=" + CatValor.ToString() + " CatFinal=" + CatFinal.ToString());
                    // Construimos el Objeto de Cotización con la Actualización del CAT
                    Entity ActualizaCotizacion = new Entity("fib_cotizador");
                    //ActualizaCotizacion.Attributes.Add("fib_cotizadorid", new EntityReference(mainTable.Attributes["fib_cotizacionid"].ToString()));
                    ActualizaCotizacion.Attributes.Add("fib_cotizadorid", fib_cotizacion.Id);
                    ActualizaCotizacion.Attributes.Add("fib_cat", (decimal)CatFinal);
                    // Usamos el Servicio de Organización para realizar la Actualización
                    servicio.Update(ActualizaCotizacion);
                    // Limpiamos la Lista de los valores del IRR
                    ValoresIIR.Clear();
                }
                PConsole.writeLine("genAmortizaTable FIN");
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                throw new InvalidPluginExecutionException("Error al actualizar: ", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error: " + ex.Message);
            }
        }

        private void setPagosProg(IOrganizationService servicio, Entity config, bool segFinanciado, List<Periodo> Seguro, decimal Comision)
        {
            try
            {
                PConsole.writeLine("setPagosProg 1");
                bool perAjuste = false;
                bool perGracia = false;
                string cadpp = config.Attributes["fib_pagosprogramados"].ToString();
                double myfrecuencia = 0;

                PConsole.writeLine("setPagosProg 2: " + PagosGen[0].GetAttributeValue<Money>("fib_capital").Value.ToString());

                string[] prevpp = cadpp.Split('|');
                var PersGracia = (from c in PagosGen
                                  where decimal.Parse(c.GetAttributeValue<Money>("fib_capital").Value.ToString()).Equals(0)
                                  select int.Parse(c.GetAttributeValue<int>("fib_periodo").ToString())).ToList();  // Aquellos periodos que son de gracia 

                PConsole.writeLine("setPagosProg 3");

                // inicializamos  los parametros iniciales
                //if (config.Attributes["fib_periododegracia"] != null && !bool.Parse(config.Attributes["fib_pergracia"].ToString()))
                if (config.Contains("fib_periododegracia") && !bool.Parse(config.Attributes["fib_pergracia"].ToString()))
                    perAjuste = true;
                else if (config.Contains("fib_pergracia") && bool.Parse(config.Attributes["fib_pergracia"].ToString()))
                    perGracia = true;

                PConsole.writeLine("setPagosProg 4");

                double tasa = double.Parse(config.Attributes["fib_tasa"].ToString());
                double freccapit = double.Parse(config.Attributes["fib_frecuenciadecapital"].ToString());
                int periodos = int.Parse(config.Attributes["fib_periodos"].ToString());   //- (( PersGracia.Count() > 1  )? PersGracia.Count() :0 )   ;
                //double monto = (double)config.fib_monto.Value + ( (config.fib_importeseguro != null )?  (double) config.fib_importeseguro.Value :0 )  ;
                Money fib_monto = (Money)config.Attributes["fib_monto"];
                //double monto = double.Parse(config.Attributes["fib_monto"].ToString());
                double monto = double.Parse(fib_monto.Value.ToString());
                double iva = double.Parse(config.Attributes["fib_iva"].ToString());
                iva = iva / 100;
                tasa = tasa / 100;

                PConsole.writeLine("setPagosProg 5");

                // tasa sin iva
                double tsiniva_all = (tasa / 360) * freccapit;
                double tsiniva = Math.Round(tsiniva_all, 6);

                // tasa con iva
                double tconiva_all = ((tasa / 360) * freccapit) * (1 + iva);
                double tconiva = Math.Round(tconiva_all, 6);
                double sumpagos = 0.0;

                PConsole.writeLine("setPagosProg 6");

                foreach (string prvpp in prevpp)
                {
                    string[] datos = prvpp.Split('=');
                    string[] numpagos = datos[0].Split(',');
                    double importe = double.Parse(datos[1]);

                    foreach (string strpg in numpagos)
                    {
                        int numpago = int.Parse(strpg);
                        if (PagosGen.Exists(x => int.Parse(x.Attributes["fib_periodo"].ToString()).Equals(numpago)))
                        {
                            int index = PagosGen.FindIndex(x => int.Parse(x.Attributes["fib_periodo"].ToString()).Equals(numpago));
                            double myfrec = double.Parse(PagosGen[index].Attributes["fib_diasdelperiodo"].ToString());
                            double tsaconiva = (tasa / 360) * (1 + iva) * myfrec;
                            int myperiodo = int.Parse(PagosGen[index].Attributes["fib_periodo"].ToString());
                            double valpresente = importe * (Math.Pow((1 + tsaconiva), -myperiodo));
                            sumpagos += Math.Round(valpresente, 2);
                        }
                    }
                }

                PConsole.writeLine("setPagosProg 7");

                int index2 = PagosGen.FindIndex(x => int.Parse(x.Attributes["fib_periodo"].ToString()).Equals(2));
                myfrecuencia = double.Parse(PagosGen[index2].Attributes["fib_diasdelperiodo"].ToString());

                double newpago = ((((monto - sumpagos) * tconiva)) / (1 - (Math.Pow((1 + tconiva), -periodos))));
                newpago = Math.Round(newpago, 2);

                double sldinicial = monto;
                double pagoAjuste = 0;

                string cadProg = "";

                PConsole.writeLine("setPagosProg 8");

                foreach (Entity pergen in PagosGen)
                {

                    // (pergen.fib_periodo.Value == 1 && perAjuste) ? myfrecuencia : (double)amort.fib_diasdelperiodo.Value;                    
                    double myfrec = (int.Parse(pergen.Attributes["fib_periodo"].ToString()) == 1 && perAjuste) ? myfrecuencia : double.Parse(pergen.Attributes["fib_diasdelperiodo"].ToString());
                    //double mytsiniva_all = (tasa / 360) * myfrec;
                    //double interes = Math.Round(sldinicial * mytsiniva_all, 2);  
                    double tdsiniva = (tasa / 360);
                    double interes = Math.Round(tdsiniva * sldinicial, 2, MidpointRounding.AwayFromZero) * myfrec;

                    double myiva = Math.Round(interes * iva, 2);
                    double mypgprog = getImpPgPrgo(prevpp, int.Parse(pergen.Attributes["fib_periodo"].ToString()));
                    double mypago = newpago + mypgprog;

                    double mycapital = (decimal.Parse(pergen.GetAttributeValue<Money>("fib_capital").Value.ToString()) == 0) ? 0 : ((int.Parse(pergen.Attributes["fib_periodo"].ToString()) == periodos) ? sldinicial : Math.Round(mypago - interes - myiva, 2));

                    double sldfinal = Math.Round(sldinicial - mycapital, 2);

                    if (int.Parse(pergen.Attributes["fib_periodo"].ToString()) == 1 && perAjuste)
                    {
                        int dias = int.Parse(pergen.Attributes["fib_diasdelperiodo"].ToString());
                        double myfrecuencia2 = getFrecPago(pergen.Attributes["fib_fecha"].ToString(), 1);
                        // pagoAjuste = Math.Round((Math.Round((tasa / 360) * (dias - myfrecuencia), 6)) * monto, 2);
                        pagoAjuste = Math.Round((tasa / 360) * monto, 2) * (dias - myfrecuencia2);
                        interes += pagoAjuste;
                        myiva = Math.Round(interes * iva, 2);
                        mypago = interes + myiva + mycapital;

                    }

                    if (PersGracia.Exists(x => x.Equals(int.Parse(pergen.Attributes["fib_periodo"].ToString()))) && perGracia)
                        mypago = interes + myiva + mycapital + mypgprog;


                    if (int.Parse(pergen.Attributes["fib_periodo"].ToString()) != 0 && mycapital < 0)
                    {
                        string message = "El pago prog. debe ser menor al que se definio, al recalcular  el capital del periodo " +
                                         int.Parse(pergen.Attributes["fib_periodo"].ToString()).ToString("00") + " da un importe negativo -" + mycapital.ToString("C") + " ";
                        result = "Error :  " + message;
                        //  throw new InvalidPluginExecutionException(message);
                        return;
                    }

                    if (int.Parse(pergen.Attributes["fib_periodo"].ToString()) == periodos)
                        mypago = mycapital + interes + myiva;

                    //pergen.Attributes.Add("fib_interes", (decimal)interes);
                    pergen.Attributes["fib_interes"] = new Money(Math.Round((decimal)interes, 2, MidpointRounding.ToEven));

                    PConsole.writeLine("setPagosProg 8.2: " + ((decimal)interes));
                    PConsole.writeLine("setPagosProg 8.3: " + pergen.Attributes["fib_interes"].ToString());

                    //pergen.Attributes["fib_capital"] = (decimal)mycapital;
                    pergen.Attributes["fib_capital"] = new Money(decimal.Parse(mycapital.ToString()));

                    //pergen.Attributes["fib_iva"] = (float)myiva;
                    pergen.Attributes["fib_iva"] = (decimal)myiva;

                    pergen.Attributes["fib_saldofinal"] = new Money((decimal)sldfinal);

                    pergen.Attributes["fib_saldoinicial"] = new Money((decimal)sldinicial);

                    //pergen.Attributes["fib_pago"] = (decimal)mypago;
                    pergen.Attributes["fib_pago"] = new Money(decimal.Parse(mypago.ToString()));

                    cadProg += int.Parse(pergen.Attributes["fib_periodo"].ToString()).ToString() + " " + (interes + mycapital).ToString() + "    ";

                    PConsole.writeLine("setPagosProg 8.8: " + Seguro.Exists(x => x.periodo.Equals(int.Parse(pergen.Attributes["fib_periodo"].ToString()))).ToString());
                    if (segFinanciado && Seguro.Exists(x => x.periodo.Equals(int.Parse(pergen.Attributes["fib_periodo"].ToString()))))
                    {
                        Periodo pgSeg = Seguro.Find(x => x.periodo.Equals(int.Parse(pergen.Attributes["fib_periodo"].ToString())));

                        PConsole.writeLine("setPagosProg 8.8.1: " + pergen.Attributes["fib_interes"].ToString());
                        pergen.Attributes["fib_saldoinicial"] = new Money(decimal.Parse(pergen.GetAttributeValue<Money>("fib_saldoinicial").Value.ToString()) + (decimal)pgSeg.saldoInicial);
                        pergen.Attributes["fib_capital"] = new Money(decimal.Parse(pergen.GetAttributeValue<Money>("fib_capital").Value.ToString()) + (decimal)pgSeg.capital);
                        pergen.Attributes["fib_interes"] = new Money(Math.Round(decimal.Parse(pergen.GetAttributeValue<Money>("fib_interes").Value.ToString()) + (decimal)pgSeg.interes, 2, MidpointRounding.ToEven));

                        PConsole.writeLine("setPagosProg 8.8.2: " + pergen.Attributes["fib_iva"].ToString());
                        pergen.Attributes["fib_iva"] = double.Parse(pergen.Attributes["fib_iva"].ToString()) + pgSeg.iva;
                        pergen.Attributes["fib_pago"] = new Money(decimal.Parse(pergen.GetAttributeValue<Money>("fib_pago").Value.ToString()) + (decimal)pgSeg.pago);
                        pergen.Attributes["fib_saldofinal"] = new Money(Math.Round(decimal.Parse(pergen.GetAttributeValue<Money>("fib_saldoinicial").Value.ToString()) - decimal.Parse(pergen.GetAttributeValue<Money>("fib_capital").Value.ToString()), 2));

                        PConsole.writeLine("setPagosProg 8.8.3");
                        pergen.Attributes["fib_segnofinanciado"] = new Money((decimal)pgSeg.pago);
                        pergen.Attributes["fib_pagomasseguro"] = new Money(decimal.Parse(pergen.GetAttributeValue<Money>("fib_pago").Value.ToString()));
                    }


                    #region Ajustes Nuevos
                    pergen.Attributes["fib_pagomasseguro"] = new Money(decimal.Parse(pergen.GetAttributeValue<Money>("fib_pago").Value.ToString()) + Comision);
                    pergen.Attributes["fib_pago"] = new Money(decimal.Parse(pergen.GetAttributeValue<Money>("fib_pago").Value.ToString()) + Comision);
                    #endregion



                    pergen.Attributes["fib_pagoprogramado"] = new Money((decimal)mypgprog);

                    PConsole.writeLine("setPagosProg " + pergen.Attributes["fib_periodo"].ToString() + ": " + pergen.GetAttributeValue<Money>("fib_interes").Value);

                    servicio.Update(pergen);

                    sldinicial = sldfinal;
                }
                PConsole.writeLine("setPagosProg 10. FIN.");
            }
            catch (InvalidPluginExecutionException ex)
            {
                PConsole.writeLine("setPagosProg EX1 : " + ex.Message);
                throw new InvalidPluginExecutionException("Error al actualizar: ", ex);
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                PConsole.writeLine("setPagosProg EX2 : " + ex.Message);
                throw new InvalidPluginExecutionException("Error al actualizar: ", ex);
            }
            catch (Exception ex)
            {
                PConsole.writeLine("setPagosProg EX3 : " + ex.Message);
                throw new InvalidPluginExecutionException("Error: " + ex.Message);
            }
        }

        private double getImpPgPrgo(string[] prevpagos, int periodo)
        {
            PConsole.writeLine("getImpPgPrgo 1: " + periodo.ToString());
            foreach (string prvpp in prevpagos)
            {
                PConsole.writeLine("getImpPgPrgo 2: " + prvpp);
                string[] datos = prvpp.Split('=');
                string[] numpagos = datos[0].Split(',');
                double importe = double.Parse(datos[1]);

                foreach (string strperiodo in numpagos)
                {
                    PConsole.writeLine("getImpPgPrgo 2: " + strperiodo);
                    int perpp = int.Parse(strperiodo);
                    if (periodo.Equals(perpp))
                    {
                        PConsole.writeLine("getImpPgPrgo 3: " + importe.ToString());
                        return importe;
                    }
                }
            }
            return 0;
        }

        private double getFrecPago(string fecha, int periodo)
        {
            DateTime myfecha = DateTime.Parse(fecha, new CultureInfo("es-MX"));
            int Annio = myfecha.Year;
            double perpago = 0;

            foreach (Entity per in PagosGen)
            {
                DateTime fechapg = DateTime.Parse(per.Attributes["fib_fecha"].ToString(), new CultureInfo("es-MX"));
                int annioPg = fechapg.Year;
                perpago = double.Parse(per.Attributes["fib_diasdelperiodo"].ToString());

                if (int.Parse(per.Attributes["fib_periodo"].ToString()) != periodo && Annio == annioPg)
                    return double.Parse(per.Attributes["fib_diasdelperiodo"].ToString());
            }
            return perpago;
        }

        private EntityCollection RecuperaPagos(IOrganizationService servicio, Guid IdCotiza)
        {
            PConsole.writeLine("RecuperaPagos 1 ");
            ConditionExpression condition = new ConditionExpression();
            condition.AttributeName = "fib_tablaamortizacinid";
            condition.Operator = ConditionOperator.Equal;
            condition.Values.Add(IdCotiza);

            // Se contruye el criterio de Filtro
            FilterExpression filter = new FilterExpression();
            filter.FilterOperator = LogicalOperator.And;
            filter.Conditions.Add(condition);

            // Ordenando por periodo - RGomezS 25/07/2013
            OrderExpression order = new OrderExpression();
            order.AttributeName = "fib_periodo";
            order.OrderType = OrderType.Ascending;

            // Se construye el Objeto de Consulta Final
            QueryExpression query = new QueryExpression();
            query.EntityName = "fib_amortizacion";
            query.ColumnSet = new ColumnSet(true);
            query.Criteria = filter;
            query.Orders.Add(order);

            try
            {
                // Se consume el Servicio de Organizacion (Recuperacion Multiple)
                EntityCollection pagos = servicio.RetrieveMultiple(query);
                PConsole.writeLine("RecuperaPagos 2 ");
                return pagos;
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                throw new InvalidPluginExecutionException("Error: ", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error: " + ex.Message);
            }
        }


        #region Seguro Multianual 2019
        /// <summary>
        /// función para traer información de la configuración del seguro
        /// </summary>
        /// <param name="servicio"></param>
        /// <param name="entidad"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        private Seguro getTiposeguro(Entity Entidadseguro)
        {
            Seguro confSeguro = new Seguro();
            // Entity Entidadseguro = servicio.Retrieve("fib_cotizador", Id, new ColumnSet(new string[] { "fib_seguro_multianual", "fib_tipo_seguro_multianual", "fib_iva_seguro", "fib_pago1_con_iva", "fib_pagos_subsecuentes_con_iva", "fib_importeseguro" }));

            bool contieneSeguroMultianual = Entidadseguro.Attributes.Contains("fib_seguro_multianual");
            confSeguro.fib_seguro_multianual = (contieneSeguroMultianual) ? Convert.ToInt32(Entidadseguro.Attributes["fib_seguro_multianual"].ToString()) : 0;

            bool contieneTipoSeguro = Entidadseguro.Attributes.Contains("fib_tipo_seguro_multianual");
            confSeguro.fib_tipo_seguro_multianual = (contieneTipoSeguro) ? Convert.ToInt32(Entidadseguro.Attributes["fib_tipo_seguro_multianual"].ToString()) : 0;

            bool contienePago1conIva = Entidadseguro.Attributes.Contains("fib_pago1_con_iva");
            confSeguro.fib_pago1_con_iva = (contienePago1conIva) ? Convert.ToDecimal(Entidadseguro.GetAttributeValue<Money>("fib_pago1_con_iva").Value.ToString()) : 0;

            bool contienePagoSubsecuente = Entidadseguro.Attributes.Contains("fib_pago_subsecuente_con_iva");
            confSeguro.fib_pago_subsecuente_con_iva = (contienePagoSubsecuente) ? Convert.ToDecimal(Entidadseguro.GetAttributeValue<Money>("fib_pago_subsecuente_con_iva").Value.ToString()) : 0;

            bool contieneImporteSeguro = Entidadseguro.Attributes.Contains("fib_importeseguro");
            confSeguro.fib_importeseguro = (contieneImporteSeguro) ? Convert.ToDecimal(Entidadseguro.GetAttributeValue<Money>("fib_importeseguro").Value.ToString()) : 0;

            bool contieneAplicaComision = Entidadseguro.Attributes.Contains("fib_aplica_comision_administracion");
            confSeguro.fib_aplica_comision_administracion = (contieneAplicaComision) ? Convert.ToInt32(Entidadseguro.Attributes["fib_aplica_comision_administracion"].ToString()) : 0;

            bool contieneIvaComision = Entidadseguro.Attributes.Contains("fib_iva_comision_administracion");
            confSeguro.fib_iva_comision_administracion = (contieneIvaComision) ? Convert.ToDecimal(Entidadseguro.Attributes["fib_iva_comision_administracion"].ToString()) : 0;

            bool contieneComision = Entidadseguro.Attributes.Contains("fib_comision_administracion_sin_iva");
            confSeguro.fib_comision_administracion_sin_iva = (contieneImporteSeguro) ? Convert.ToDecimal(Entidadseguro.GetAttributeValue<Money>("fib_comision_administracion_sin_iva").Value.ToString()) : 0;

            return confSeguro;
        }

        #endregion


    }
}
