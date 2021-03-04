using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Troschuetz.Random;
using Distributions.Distributions;
using System.Windows.Forms.DataVisualization.Charting;

namespace Distributions {
    public enum TestType { BoxMuller, Ziggurat }

    public partial class MainForm : Form {
        private DiscreteUniformDistribution uniformDistr = new DiscreteUniformDistribution();
        private ExponentialDistribution expDistr = new ExponentialDistribution();
        private NormalDistribution normalDistr = new NormalDistribution();

        private const int SAMPLES = 1000000;

        private int testNumber;

        public MainForm() {
            InitializeComponent();

            BuildUniform();
            BuildExponential();
            BuildNormal();



            //MyRandom mr = new MyRandom();

            //var t1 = DateTime.Now;
            //for (int i = 0; i < 1000000; i++) {
            //    mr.BoxMullerDouble();
            //}
            //double res1 = (DateTime.Now - t1).TotalMilliseconds;

            //t1 = DateTime.Now;
            //for (int i = 0; i < 1000000; i++) {
            //    mr.Sample();
            //}
            //double res2 = (DateTime.Now - t1).TotalMilliseconds;

            //MessageBox.Show($"{res1} - box; {res2} - zik");

        }

        private void BuildUniform() {
            // Гистограмма плотности
            Dictionary<double, double> dict = new Dictionary<double, double>();

            uniformDistr.Alpha = 0;
            uniformDistr.Beta = 50;

            FillDictionary(ref dict, uniformDistr);
            FillChart(dict, uniformGist);

            // Функция распределения
            uniformFunc.ChartAreas[0].AxisX.Minimum = uniformDistr.Minimum - 20;
            uniformFunc.ChartAreas[0].AxisX.Maximum = uniformDistr.Maximum + 20;

            FillUnifromFunctionChart();
        }
        
        private void BuildExponential() {
            // Гистограмма плотности
            Dictionary<double, double> dict = new Dictionary<double, double>();

            FillDictionary(ref dict, expDistr);

            // Нормализация (Небольшой костыль)
            dict[0] += 0.05;

            FillChart(dict, expGist);

            // Функция распределения
            expFunc.ChartAreas[0].AxisX.Minimum = 0;
            expFunc.ChartAreas[0].AxisX.Maximum = 10;

            FillExpFunctionChart();
        }

        private void BuildNormal() {
            // Гистограмма плотности
            Dictionary<double, double> dict = new Dictionary<double, double>();

            normalDistr.Mu = 0;

            FillDictionary(ref dict, normalDistr);
            FillChart(dict, normalGist);

            // Функция распределения
            expFunc.ChartAreas[0].AxisX.Minimum = 0;
            expFunc.ChartAreas[0].AxisX.Maximum = 10;

            FillNormalFunctionChart();
        }

        private void FillDictionary(ref Dictionary<double, double> dict, Distribution distribution) {
            for (int i = 0; i < SAMPLES; i++) {
                int round = (distribution is DiscreteUniformDistribution) ? 2 : 1;
                double r = Math.Round(distribution.NextDouble(), round);
                if (dict.ContainsKey(r))
                    dict[r] = Math.Round(dict[r] + (1 / (double)SAMPLES), 6);
                else
                    dict[r] = 1 / (double)SAMPLES;
            }
        }

        private void FillChart(Dictionary<double, double> dict, Chart chart) {
            chart.Series[0].Points.DataBindXY(dict.Keys, dict.Values);
        }

        private void FillUnifromFunctionChart() {
            Dictionary<double, double> points = new Dictionary<double, double>();

            points.Add(-20, 0);
            points.Add(uniformDistr.Minimum, 0);
            points.Add(uniformDistr.Maximum, 1);
            points.Add(uniformDistr.Maximum + 20, 1);

            FillChart(points, uniformFunc);
        }

        private void FillExpFunctionChart() {
            Dictionary<double, double> points = new Dictionary<double, double>();
            Func<double, double> func = x => x < 0 ? 0 : 
                1 - Math.Pow(Math.E, -expDistr.Lambda * x);

            for (int i = 0; i <= 10; i++) {
                points.Add(i, func(i));
            }

            FillChart(points, expFunc);
        }

        private void FillNormalFunctionChart() {
            Dictionary<double, double> points = new Dictionary<double, double>();
            Func<double, double, double, double> func = (x, u, q) => 
                (1 + AdvancedMath.Erf((x - u) / Math.Sqrt(2 * Math.Pow(q, 2)))) / 2;

            for (int i = -3; i <=3; i++) {
                points.Add(i, func(i, 0, 1));
            }

            FillChart(points, normalFunc);
        }

        private void Button1_Click(object sender, EventArgs e) {
            testNumber = Convert.ToInt32(testNumberBox.Text);
            double boxMuller = DoTest(r => r.BoxMullerDouble());
            double ziggurat = DoTest(r => r.Sample());
            boxMullerResult.Text = boxMuller.ToString();
            zigguratResult.Text = ziggurat.ToString();
        }

        private double DoTest(Func<MyRandom, double> f) {
            MyRandom mr = new MyRandom();
            DateTime now = DateTime.Now;
            for (int i = 0; i < testNumber; i++) {
                f(mr);
            }
            return (DateTime.Now - now).TotalMilliseconds;
        }
    }
}
