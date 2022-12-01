using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace buildTabla2
{
    class Periodo
    {
        public int periodo { set; get; }
        public DateTime fecha { set; get; }
        public double saldoInicial { set; get; }
        public double capital { set; get; }
        public double interes { set; get; }
        public double iva { set; get; }
        public double pago { set; get; }
        public double saldofinal { set; get; }
        public double freccapital { set; get; }
        public double tsiniva { set; get; }
        public bool update { set; get; }
    }
}
