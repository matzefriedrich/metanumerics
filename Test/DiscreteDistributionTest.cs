﻿using System;
using System.Diagnostics;


using Meta.Numerics;
using Meta.Numerics.Statistics;
using Meta.Numerics.Statistics.Distributions;


namespace Test {
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestResult = Meta.Numerics.Statistics.TestResult;

    [TestClass]
    public class DiscreteDistributionTest {

        private DiscreteDistribution[] distributions = GetDistributions();

        public static DiscreteDistribution[] GetDistributions () {
            return (new DiscreteDistribution[] {
                new BernoulliDistribution(0.1),
                new BinomialDistribution(0.2, 30), new BinomialDistribution(0.4, 5),
                new PoissonDistribution(4.5), new PoissonDistribution(400.0),
                new DiscreteUniformDistribution(5, 11),
                new GeometricDistribution(0.6),
                new NegativeBinomialDistribution(7.8, 0.4),
                new HypergeometricDistribution(9, 3, 5)
            });
        }

        [TestMethod]
        public void DiscreteDistributionUnitarity () {
            foreach (DiscreteDistribution distribution in distributions) {
                Assert.IsTrue(TestUtilities.IsNearlyEqual(
                    distribution.ExpectationValue(delegate (int k) { return (1.0); }), 1.0
                ));
            }
        }

        [TestMethod]
        public void DiscreteDistributionMean () {
            foreach (DiscreteDistribution distribution in distributions) {
                Assert.IsTrue(TestUtilities.IsNearlyEqual(
                    distribution.ExpectationValue(delegate(int k) { return (k); }), distribution.Mean
                ));
            }
        }

        [TestMethod]
        public void DiscreteDistributionVariance () {
            foreach (DiscreteDistribution distribution in distributions) {
                double m = distribution.Mean;
                Assert.IsTrue(TestUtilities.IsNearlyEqual(
                    distribution.ExpectationValue(delegate(int x) { return (Math.Pow(x-m, 2)); }), distribution.Variance
                ));
            }
        }

        [TestMethod]
        public void DiscreteDistributionProbabilityAxioms () {

            foreach (DiscreteDistribution distribution in distributions) {

                // some of these values will be outside the support, but that's fine, our results should still be consistent with probability axioms
                foreach (int k in TestUtilities.GenerateUniformIntegerValues(-10, +100, 8)) {

                    Console.WriteLine("{0} {1}", distribution.GetType().Name, k);

                    double DP = distribution.ProbabilityMass(k);
                    Assert.IsTrue(DP >= 0.0); Assert.IsTrue(DP <= 1.0);

                    double P = distribution.LeftInclusiveProbability(k);
                    double Q = distribution.RightExclusiveProbability(k);
                    Console.WriteLine("{0} {1} {2}", P, Q, P + Q);

                    Assert.IsTrue(P >= 0.0); Assert.IsTrue(P <= 1.0);
                    Assert.IsTrue(Q >= 0.0); Assert.IsTrue(Q <= 1.0);
                    Assert.IsTrue(TestUtilities.IsNearlyEqual(P + Q, 1.0));

                }

            }

        }

        [TestMethod]
        public void DiscreteDistributionInverseCDF () {

            Random rng = new Random(1);
            for (int i = 0; i < 10; i++) {

                double P = rng.NextDouble();

                foreach (DiscreteDistribution distribution in distributions) {
                    int k = distribution.InverseLeftProbability(P);
                    Console.WriteLine("{0} P={1} K={2} P(k<K)={3} P(k<=K)={4}", distribution.GetType().Name, P, k, distribution.LeftExclusiveProbability(k), distribution.LeftInclusiveProbability(k));
                    Console.WriteLine("    {0} {1}", distribution.LeftExclusiveProbability(k) < P, P <= distribution.LeftInclusiveProbability(k));
                    Assert.IsTrue(distribution.LeftExclusiveProbability(k) < P);
                    Assert.IsTrue(P <= distribution.LeftInclusiveProbability(k));
                }


            }

        }

        [TestMethod]
        public void BinomialNegativeBinomialRelation () {

            int k = 2;

            int r = 3;
            double p = 0.4;
            NegativeBinomialDistribution nb = new NegativeBinomialDistribution(r, p);

            int n = r + k;
            BinomialDistribution b = new BinomialDistribution(p, n);

            double nbP = nb.LeftInclusiveProbability(k);
            double bP = b.LeftInclusiveProbability(k);


        }

        /*
        [TestMethod]
        public void DiscreteContinuousAgreement () {

            DiscreteDistribution dd = new BinomialDistribution(0.6, 7);
            Distribution cd = new DiscreteAsContinuousDistribution(dd);

            Assert.IsTrue(cd.Mean == dd.Mean);
            Assert.IsTrue(cd.StandardDeviation == dd.StandardDeviation);
            Assert.IsTrue(cd.Variance == dd.Variance);
            Assert.IsTrue(cd.Skewness == dd.Skewness);
            Assert.IsTrue(cd.Moment(5) == dd.Moment(5));
            Assert.IsTrue(cd.MomentAboutMean(5) == dd.MomentAboutMean(5));

            // this should cause an interval conversion
            Assert.IsTrue(TestUtilities.IsNearlyEqual(cd.Support.LeftEndpoint, dd.Minimum));
            Assert.IsTrue(TestUtilities.IsNearlyEqual(cd.Support.RightEndpoint, dd.Maximum));

            //Assert.IsTrue(cd.LeftProbability(4.5) == dd.LeftProbability(4));
            //Assert.IsTrue(cd.RightProbability(4.5) == dd.RightProbability(4));

            // Switch LeftProbablity for discrete distributions to be exclusive.
            // This is already the case for internal distributions used for exact null distributions, but not for public discrete distributions.

        }
        */

        [TestMethod]
        public void OutsideDiscreteDistributionSupport () {
            foreach (DiscreteDistribution distribution in distributions) {
                int min = distribution.Support.LeftEndpoint;
                int max = distribution.Support.RightEndpoint;
                if (min > Int32.MinValue) {
                    Assert.IsTrue(distribution.ProbabilityMass(min - 1) == 0.0);
                    Assert.IsTrue(distribution.LeftInclusiveProbability(min - 1) == 0.0);
                    Assert.IsTrue(distribution.RightExclusiveProbability(min - 1) == 1.0);
                    Assert.IsTrue(distribution.LeftExclusiveProbability(min) == 0.0);
                }
                if (distribution.Support.RightEndpoint < Int32.MaxValue) {
                    Assert.IsTrue(distribution.ProbabilityMass(max + 1) == 0.0);
                    Assert.IsTrue(distribution.LeftInclusiveProbability(max + 1) == 1.0);
                    Assert.IsTrue(distribution.RightExclusiveProbability(max) == 0.0);
                }
            }
        }

        [TestMethod]
        public void PoissonBug () {

            PoissonDistribution pd = new PoissonDistribution(0.5);
            double x = pd.InverseLeftProbability(0.7716);
            Console.WriteLine(x);

        }

        [TestMethod]
        public void DiscreteDistributionRandomValues () {

            foreach (DiscreteDistribution distribution in distributions) {

                int max = distribution.Support.RightEndpoint;
                if (max < 128) {
                    max = max + 1;
                } else {
                    max = (int) Math.Round(distribution.Mean + 2.0 * distribution.StandardDeviation);
                }
                Console.WriteLine("{0} {1}", distribution.GetType().Name, max);

                Histogram h = new Histogram(max);

                Random rng = new Random(314159265);
                for (int i = 0; i < 1024; i++) h.Add(distribution.GetRandomValue(rng));
                TestResult result = h.ChiSquaredTest(distribution);
                Console.WriteLine("{0} {1}", result.Statistic, result.RightProbability);
                Assert.IsTrue(result.RightProbability > 0.05);

            }

        }

        [TestMethod]
        public void DiscreteDistributionBase () {

            DiscreteDistribution D = new DiscreteTestDistribution();

            double M0 = D.ExpectationValue(k => 1.0);
            Assert.IsTrue(TestUtilities.IsNearlyEqual(M0, 1.0));
            Assert.IsTrue(TestUtilities.IsNearlyEqual(M0, D.RawMoment(0)));

            double M1 = D.ExpectationValue(k => k);
            Assert.IsTrue(TestUtilities.IsNearlyEqual(M1, D.Mean));
            Assert.IsTrue(TestUtilities.IsNearlyEqual(M1, D.RawMoment(1)));

            double C2 = D.ExpectationValue(k => MoreMath.Sqr(k - M1));
            Assert.IsTrue(TestUtilities.IsNearlyEqual(C2, D.Variance));
            Assert.IsTrue(TestUtilities.IsNearlyEqual(C2, D.CentralMoment(2)));

            Assert.IsTrue(D.InverseLeftProbability(D.LeftInclusiveProbability(2)) == 2);

        }

    }

    // a minimal implementation to test base methods on abstract DiscreteDistribution class

    public class DiscreteTestDistribution : DiscreteDistribution {

        public override DiscreteInterval Support {
            get { return DiscreteInterval.FromEndpoints(1, 3); }
        }

        public override double ProbabilityMass (int k) {
            switch (k) {
                case 1:
                    return (1.0 / 6.0);
                case 2:
                    return (2.0 / 6.0);
                case 3:
                    return (3.0 / 6.0);
                default:
                    return (0.0);
            }
        }

    }

}
