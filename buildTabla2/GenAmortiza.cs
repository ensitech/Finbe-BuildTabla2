using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace buildTabla2
{
    class GenAmortiza
    {
        #region variables

        private int frecuencia;
        private double monto;
        private double tasa;
        private double iva;
        private int periodos;
        private DateTime fecinicio;
        private DateTime fecgracia;
        private double pago;
        private int dias;
        private int annio;
        private double freccapit;
        private bool pergracia;
        private int numdiasgraci;
        private List<Periodo> Pagos = new List<Periodo>();
        private double tsiniva = 0;
        private double tconiva = 0;
        private double tdsiniva = 0;
        private double tsiniva_all = 0;                    // tasas con todos los decimales
        private double tconiva_all = 0;                   // tasas con todos los decimales
        private double tdsiniva_all = 0;
        private bool anualidad;
        private double frecpago = 0;
        private DateTime NullDate = DateTime.MinValue;
        private bool perAjuste = false;                  // para indicar si hay periodo de ajueste solo cuando es anualidad
        private double pagoAjuste = 0;                       // pago de ajuste
        private bool credFinan = false;
        private int numPerGracia = 0;                    // num de periodos de gracia  para permitir varios periodos de gracia

        #endregion

        public double freccinipago
        {
            get { return frecpago; }
            set { frecpago = value; }
        }

        public void configTable(int frecuencia, bool anualidad, double monto, double tasa, double iva, int periodos, DateTime fecinicio, bool pergracia, DateTime fecgracia, bool credFinan, int numpergracia)
        {
            this.frecuencia = frecuencia;
            this.monto = monto;
            this.tasa = tasa;
            this.iva = iva;
            this.periodos = periodos;
            this.fecinicio = fecinicio;
            this.pergracia = pergracia;
            this.fecgracia = fecgracia;
            this.anualidad = anualidad;
            this.credFinan = credFinan;
            this.numPerGracia = numpergracia;

            initParams();
        }

        private void initParams()
        {
            this.tasa = tasa / 100;
            this.iva = iva / 100;
            if (this.pergracia)
            {
                TimeSpan ts = fecgracia.Date - fecinicio.Date;
                this.numdiasgraci = ts.Days;
            }

            double prevop = 0;
            annio = (this.fecgracia != NullDate) ? fecgracia.Year : fecinicio.Year;
            dias = (esBisiesto(annio)) ? 366 : 365;
            dias = (this.credFinan) ? 360 : dias;   // para creditos antiguos en el FINAN siempre es 360 

            if (this.anualidad)
                setFrecCapital(true);
            else
            {
                TimeSpan ts;

                if (fecgracia != NullDate && !this.pergracia)
                {
                    this.perAjuste = true; // tiene periodo de ajuste para saldos Isolutos
                    ts = fecgracia.Date - fecinicio.Date;
                    fecinicio = fecgracia;
                }
                else
                {
                    if (this.pergracia)
                        ts = fecgracia.Date - fecinicio.Date;
                    else
                        ts = incrementFecha(fecinicio).Date - fecinicio.Date;
                }

                freccapit = ts.Days;
            }

            freccinipago = freccapit;
            // tasa sin iva
            tsiniva_all = (tasa / 360) * freccapit;
            tsiniva = Math.Round(tsiniva_all, 6);
            // tasa con iva
            tconiva_all = ((tasa / 360) * freccapit) * (1 + iva);
            tconiva = Math.Round(tconiva_all, 6);
            // pago
            double potencia = (double)periodos;
            int myperiodos = this.periodos - ((this.numPerGracia > 1) ? this.numPerGracia : 0);
            // RGomezS - 11/Marzo/2016 - No se genera tabla de amortización con tasa 0
            prevop = ((1 - (Math.Pow((1 + tconiva), -myperiodos))) > 0) ? (((monto * tconiva)) / (1 - (Math.Pow((1 + tconiva), -myperiodos)))) : (double)0;
            PConsole.writeLine("prevop: " + prevop.ToString());
            pago = Math.Round(prevop, 2);
            // tasa diaria sin iva
            tdsiniva_all = tasa / 360;
            tdsiniva = tdsiniva_all; // Math.Round(tdsiniva_all, 2); // se redonde a 2

            if (this.anualidad && this.fecgracia != NullDate && !this.pergracia)
                checkPerAjuste();
        }

        public List<Periodo> createTable()
        {

            int myperiodos = this.periodos - ((this.numPerGracia > 1) ? this.numPerGracia : 0);
            DateTime fechapago = (this.pergracia) ? fecinicio.AddDays(this.numdiasgraci) : (this.perAjuste) ? fecinicio : incrementFecha(fecinicio);
            double sldinicial = this.monto;
            double mypago = this.pago;  // pago ajuste si así se requiere en caso contrario es 0
            double myinteres = (this.pergracia) ?
                Math.Round(this.monto * tdsiniva, 2) * this.numdiasgraci : Math.Round(sldinicial * tdsiniva, 2) * freccapit;
            double myiva = Math.Round(myinteres * iva, 2);
            //PConsole.writeLine("pergracia " + this.pergracia.ToString() + " anualidad " + this.anualidad.ToString() + " mypago - myinteres - myiva " + mypago.ToString() + " " + myinteres.ToString() + " " + myiva.ToString() + " monto/myperiodos " + this.monto.ToString() + "/" + myperiodos.ToString());
            double mycapital = (this.pergracia) ? 0 :
                               (this.anualidad) ? Math.Round((mypago - myinteres - myiva), 2) : Math.Round(this.monto / myperiodos, 2);
            double sldfinal = Math.Round(sldinicial - mycapital, 2);

            if (this.perAjuste)
            {
                myinteres += this.pagoAjuste;
                myiva = Math.Round(myinteres * iva, 2);
                mypago = myinteres + myiva + mycapital;
            }

            //inicializamos  la bandera para saber durante cuantos periodos no se cobra capital;
            bool multiplePerGracia = (this.pergracia && this.numPerGracia > 1);
            int inicio = (this.pergracia && !multiplePerGracia) ? 0 : 1;
            if (!this.anualidad)
                this.numdiasgraci = (int)freccapit;

            for (int i = inicio; i <= periodos; i++)
            {
                Pagos.Add(new Periodo
                {
                    periodo = i,
                    fecha = fechapago,
                    saldoInicial = sldinicial,
                    capital = mycapital,
                    interes = myinteres,
                    iva = myiva,
                    pago = (i == 0 || !this.anualidad || multiplePerGracia) ? Math.Round(mycapital + myinteres + myiva, 2) : mypago,
                    saldofinal = sldfinal,
                    freccapital = (i == 0 || ((this.perAjuste || multiplePerGracia) && i == 1)) ? this.numdiasgraci : freccapit,
                    tsiniva = (i == 0) ? this.tdsiniva : this.tsiniva,
                });

                fecinicio = fechapago;                              //fecha de inicio se vuelve la fecha de pago anterior
                fechapago = incrementFecha(fechapago);
                if (!this.anualidad)
                {
                    TimeSpan ts = fechapago.Date - fecinicio.Date;
                    freccapit = ts.Days;
                }

                updateTasa(fechapago.Year);
                sldinicial = sldfinal;

                multiplePerGracia = (multiplePerGracia) ? ((i + 1 <= this.numPerGracia) ? true : false) : false;  // actualizamos la bandera ; 
                // si es que tiene multiple periodos de gracia

                if (i + 1 == 2 && this.perAjuste && this.anualidad) // para el periodo de ajuste;
                {
                    int mynewper = periodos - 1;
                    double mymonto = sldinicial;
                    recalcPago(mynewper, mymonto);
                    mypago = this.pago;
                }

                // myinteres = Math.Round(sldinicial * this.tsiniva_all, 2);
                myinteres = Math.Round(sldinicial * this.tdsiniva, 2) * freccapit;
                myiva = Math.Round(myinteres * iva, 2);
                mycapital = (this.anualidad) ? ((i + 1 == periodos) ? sldinicial : Math.Round((mypago - myinteres - myiva), 2)) : ((i + 1 == periodos) ? sldinicial : Math.Round(this.monto / myperiodos, 2));
                mycapital = (multiplePerGracia) ? 0 : mycapital;
                sldfinal = (multiplePerGracia) ? sldinicial : Math.Round(sldinicial - mycapital, 2);
                mypago = (this.anualidad && i + 1 == periodos) ? Math.Round(mycapital + myinteres + myiva, 2) : mypago;

            }

            return Pagos;
        }

        /// <summary>
        /// Crea la tabla de seguros con el monto del 1erpago y pagos subsecuentes
        /// </summary>
        /// <returns></returns>
        public List<Periodo> createTable2(int periodosSeguro)
        {

            int myperiodos = this.periodos - ((this.numPerGracia > 1) ? this.numPerGracia : 0);
            DateTime fechapago = (this.pergracia) ? fecinicio.AddDays(this.numdiasgraci) : (this.perAjuste) ? fecinicio : incrementFecha(fecinicio);
            double sldinicial = this.monto;
            double mypago = this.pago;  // pago ajuste si así se requiere en caso contrario es 0
            double myinteres = (this.pergracia) ?
                Math.Round(this.monto * tdsiniva, 2) * this.numdiasgraci : Math.Round(sldinicial * tdsiniva, 2) * freccapit;
            double myiva = Math.Round(myinteres * iva, 2);
            //PConsole.writeLine("pergracia " + this.pergracia.ToString() + " anualidad " + this.anualidad.ToString() + " mypago - myinteres - myiva " + mypago.ToString() + " " + myinteres.ToString() + " " + myiva.ToString() + " monto/myperiodos " + this.monto.ToString() + "/" + myperiodos.ToString());
            double mycapital = (this.pergracia) ? 0 :
                               (this.anualidad) ? Math.Round((mypago - myinteres - myiva), 2) : Math.Round(this.monto / myperiodos, 2);
            double sldfinal = Math.Round(sldinicial - mycapital, 2);

            if (this.perAjuste)
            {
                myinteres += this.pagoAjuste;
                myiva = Math.Round(myinteres * iva, 2);
                mypago = myinteres + myiva + mycapital;
            }

            //inicializamos  la bandera para saber durante cuantos periodos no se cobra capital;
            bool multiplePerGracia = (this.pergracia && this.numPerGracia > 1);
            int inicio = (this.pergracia && !multiplePerGracia) ? 0 : 1;
            if (!this.anualidad)
                this.numdiasgraci = (int)freccapit;

            for (int i = inicio; i <= periodos; i++)
            {
                Pagos.Add(new Periodo
                {
                    periodo = i,
                    fecha = fechapago,
                    saldoInicial = sldinicial,
                    capital = mycapital,
                    interes = myinteres,
                    iva = myiva,
                    pago = (i == 0 || !this.anualidad || multiplePerGracia) ? Math.Round(mycapital + myinteres + myiva, 2) : mypago,
                    saldofinal = sldfinal,
                    freccapital = (i == 0 || ((this.perAjuste || multiplePerGracia) && i == 1)) ? this.numdiasgraci : freccapit,
                    tsiniva = (i == 0) ? this.tdsiniva : this.tsiniva,
                });

                fecinicio = fechapago;                              //fecha de inicio se vuelve la fecha de pago anterior
                fechapago = incrementFecha(fechapago);
                if (!this.anualidad)
                {
                    TimeSpan ts = fechapago.Date - fecinicio.Date;
                    freccapit = ts.Days;
                }

                updateTasa(fechapago.Year);
                sldinicial = sldfinal;

                multiplePerGracia = (multiplePerGracia) ? ((i + 1 <= this.numPerGracia) ? true : false) : false;  // actualizamos la bandera ; 
                // si es que tiene multiple periodos de gracia

                if (i + 1 == 2 && this.perAjuste && this.anualidad) // para el periodo de ajuste;
                {
                    int mynewper = periodos - 1;
                    double mymonto = sldinicial;
                    recalcPago(mynewper, mymonto);
                    mypago = this.pago;
                }

                // myinteres = Math.Round(sldinicial * this.tsiniva_all, 2);
                myinteres = Math.Round(sldinicial * this.tdsiniva, 2) * freccapit;
                myiva = Math.Round(myinteres * iva, 2);
                mycapital = (this.anualidad) ? ((i + 1 == periodos) ? sldinicial : Math.Round((mypago - myinteres - myiva), 2)) : ((i + 1 == periodos) ? sldinicial : Math.Round(this.monto / myperiodos, 2));
                mycapital = (multiplePerGracia) ? 0 : mycapital;
                sldfinal = (multiplePerGracia) ? sldinicial : Math.Round(sldinicial - mycapital, 2);
                mypago = (this.anualidad && i + 1 == periodos) ? Math.Round(mycapital + myinteres + myiva, 2) : mypago;

            }

            return Pagos;
        }

        public List<Periodo> createTableUnPago()
        {

            DateTime fechapago = (this.pergracia || this.perAjuste) ? (this.perAjuste) ? fecinicio : fecinicio.AddDays(this.numdiasgraci) : incrementFecha(this.fecinicio);
            double sldinicial = this.monto;
            double sldfinal = (periodos == 1) ? 0 : sldinicial;
            double myinteres = (this.pergracia || this.perAjuste) ?
                                Math.Round(this.monto * tdsiniva, 2) * this.numdiasgraci : Math.Round(sldinicial * tdsiniva, 2) * freccapit;
            double myiva = Math.Round(myinteres * this.iva, 2);
            double mycapital = (periodos == 1) ? sldinicial : 0;
            double mypago = mycapital + myinteres + myiva;

            for (int i = 1; i <= periodos; i++)
            {

                Pagos.Add(new Periodo
                {

                    periodo = i,
                    fecha = fechapago,
                    saldoInicial = sldinicial,
                    capital = mycapital,
                    interes = myinteres,
                    iva = myiva,
                    pago = mypago,
                    saldofinal = sldfinal,
                    freccapital = (i == 1 && (this.pergracia || this.perAjuste)) ? this.numdiasgraci : freccapit,
                    tsiniva = this.tsiniva
                });

                fechapago = incrementFecha(fechapago);
                updateTasa(fechapago.Year);
                // myinteres = Math.Round(sldinicial * tsiniva_all, 2);
                myinteres = Math.Round(sldinicial * tdsiniva, 2) * freccapit;
                myiva = Math.Round(myinteres * this.iva, 2);
                mycapital = (i + 1 == periodos) ? sldinicial : 0;
                mypago = mycapital + myiva + myinteres;
                sldfinal = (i + 1 == periodos) ? sldinicial - mycapital : sldinicial;

            }

            return Pagos;
        }

        private string SerializarToXml()
        {
            try
            {
                StringWriter strWriter = new StringWriter();
                XmlSerializer serializer = new XmlSerializer(Pagos.GetType());

                serializer.Serialize(strWriter, Pagos);
                string resultXml = strWriter.ToString();
                strWriter.Close();

                return resultXml;
            }
            catch
            {
                return string.Empty;
            }

        }

        private void setFrecCapital(bool prinvez)
        {
            if (frecuencia <= 15)
                freccapit = frecuencia;
            else
            {
                if (frecuencia == 30)
                {
                    freccapit = Math.Round((double)dias / 12, 4); // modiiif
                }
                else if (frecuencia == 90)
                {
                    freccapit = Math.Round((double)dias / 4, 4);
                }
            }
        }

        private DateTime incrementFecha(DateTime fecha)
        {
            switch (frecuencia)
            {
                case 30:
                    fecha = fecha.AddMonths(1);
                    break;
                case 90:
                    fecha = fecha.AddMonths(3);
                    break;
                default:
                    fecha = fecha.AddDays(frecuencia);
                    break;
            }
            return fecha;
        }

        private void updateTasa(int myannio)
        {
            if (this.anualidad)
                checkAnnio(myannio);
            else
            {
                if (esBisiesto(myannio))
                    this.dias = 366;
                else
                    this.dias = 365;

                this.dias = (this.credFinan) ? 360 : this.dias;

                tsiniva_all = (tasa / 360) * freccapit;
                tsiniva = Math.Round(tsiniva_all, 6);
                // tasa con iva
                tconiva_all = ((tasa / 360) * freccapit) * (1 + iva);
                tconiva = Math.Round(tconiva_all, 6);
            }
        }

        private void checkPerAjuste()
        {
            if (this.fecgracia != NullDate && !this.pergracia)
            {
                this.perAjuste = true;
                TimeSpan ts = fecgracia.Date - fecinicio.Date;
                //this.pagoAjuste = Math.Round((Math.Round((this.tasa / 360) * (ts.Days - this.freccapit), 6)) * this.monto, 2);
                this.pagoAjuste = Math.Round((this.tasa / 360) * this.monto, 2) * (ts.Days - this.freccapit);
                this.numdiasgraci = ts.Days;
                this.fecinicio = this.fecgracia;
            }
        }

        private void recalcPago(int newpers, double mymonto)
        {
            double prevop = (((mymonto * tconiva)) / (1 - (Math.Pow((1 + tconiva), -newpers))));
            this.pago = Math.Round(prevop, 2);
        }

        private void checkAnnio(int myannio)
        {
            if (esBisiesto(myannio))
            {
                this.dias = 366;
                this.annio = myannio;
            }
            else
            {
                this.dias = 365;
            }
            // se recalcula la tasa sin iva, con iva, frecuencia capital etc.

            // frecuencia de capital
            this.dias = (this.credFinan) ? 360 : this.dias;

            setFrecCapital(false);
            // tasa sin iva
            tsiniva_all = (tasa / 360) * freccapit;
            tsiniva = Math.Round(tsiniva_all, 6);
            // tasa con iva
            tconiva_all = ((tasa / 360) * freccapit) * (1 + iva);
            tconiva = Math.Round(tconiva_all, 6);
        }

        private bool esBisiesto(int annio)
        {
            return ((annio % 4 == 0 && annio % 100 != 0) || annio % 400 == 0);
        }
    }
}
