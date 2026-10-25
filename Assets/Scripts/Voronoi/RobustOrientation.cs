//https://dl.acm.org/doi/pdf/10.1145/237218.237337
//https://www.cs.cmu.edu/afs/cs/project/quake/public/code/predicates.c
//implementation of adaptive orientation functions from Jonathan Shewchuk in C#
using UnityEngine;

namespace RobustOrientation{
    public static class RO{
    
        private static readonly double epsilon;
        private static readonly double resulterrbound, ccwerrboundA, ccwerrboundB, ccwerrboundC;
        private static double splitSplitter = (1 << 27) + 1.0;
        //public void exactinit() in Shewchuk's implementation
        //I change this to a constructor so that we can stay static 
        static RO()
        {
            //epsilon is repeatedly halved until 1.0 + epsilon = 1.0
            //then we find our error values using epsilon
            double half;
            double check, lastcheck;
            bool every_other;
            every_other = true;
            half = 0.5;
            epsilon = 1.0;
            double splitter = 1.0;
            check = 1.0;
            /* Repeatedly divide `epsilon' by two until it is too small to add to    */
            /*   one without causing roundoff.  (Also check if the sum is equal to   */
            /*   the previous sum, for machines that round up instead of using exact */
            /*   rounding.  Not that this library will work on such machines anyway. */
            do {
                lastcheck = check;
                epsilon *= half;
                if (every_other) {
                splitter *= 2.0;
                }
                every_other = !every_other;
                check = 1.0 + epsilon;
            } while ((check != 1.0) && (check != lastcheck));
            splitter += 1.0;

            /* Error bounds for orientation and incircle tests. */
            //we will only use and uncomment the ones used for orientation

            resulterrbound = (3.0 + 8.0 * epsilon) * epsilon;
            ccwerrboundA = (3.0 + 16.0 * epsilon) * epsilon;
            ccwerrboundB = (2.0 + 12.0 * epsilon) * epsilon;
            ccwerrboundC = (9.0 + 64.0 * epsilon) * epsilon * epsilon;
            //o3derrboundA = (7.0 + 56.0 * epsilon) * epsilon;
            //o3derrboundB = (3.0 + 28.0 * epsilon) * epsilon;
            //o3derrboundC = (26.0 + 288.0 * epsilon) * epsilon * epsilon;
            //iccerrboundA = (10.0 + 96.0 * epsilon) * epsilon;
            //iccerrboundB = (4.0 + 48.0 * epsilon) * epsilon;
            //iccerrboundC = (44.0 + 576.0 * epsilon) * epsilon * epsilon;
            //isperrboundA = (16.0 + 224.0 * epsilon) * epsilon;
            //isperrboundB = (5.0 + 72.0 * epsilon) * epsilon;
            //isperrboundC = (71.0 + 1408.0 * epsilon) * epsilon * epsilon;
        }

        //Macros in predicates.c
        //when calling one of these, caller also has to specify out on out variables
        
        //cant be a typical macro, as it needs to read a before assigning
        //also because it is multiplied when called, so it cant ret void
        private static double Absolute(double a) 
        {
            return (a >= 0.0) ? a : -a;
        }

        private static void Fast_Two_Sum_Tail(double a, double b, double x, out double y)
        {
            double bvirt = x - a;
            y = b - bvirt;
        }

        private static void Fast_Two_Sum(double a, double b, out double x, out double y)
        {
            x = a + b;
            Fast_Two_Sum_Tail(a, b, x, out y);
        }

        private static void Two_Sum_Tail(double a, double b, double x, out double y)
        {
            double bvirt = x - a; 
            double avirt = x - bvirt; 
            double bround = b - bvirt; 
            double around = a - avirt; 
            y = around + bround;
        }   

        private static void Two_Sum(double a, double b, out double x, out double y)
        {
            x = a + b;
            Two_Sum_Tail(a, b, x, out y);
        }

        private static void Two_Diff_Tail(double a, double b, double x, out double y)
        {
            double bvirt = a - x; 
            double avirt = x + bvirt; 
            double bround = bvirt - b; 
            double around = a - avirt; 
            y = around + bround;
        }

        private static void Two_Diff(double a, double b, out double x, out double y) 
        {
            x = a - b; 
            Two_Diff_Tail(a, b, x, out y);
        }

        private static void Split(double a, out double ahi, out double alo)
        {
            double c = splitSplitter * a;
            double abig = c - a;
            ahi = c - abig;
            alo = a - ahi;
        }
        
        private static void Two_Product_Tail(double a, double b, double x, out double y)
        {
            double ahi, alo, bhi, blo;

            Split(a, out ahi, out alo);
            Split(b, out bhi, out blo);
            double err1 = x - (ahi * bhi);
            double err2 = err1 - (alo * bhi);
            double err3 = err2 - (ahi * blo);
            y = (alo * blo) - err3;
        }
        
        private static void Two_Product(double a, double b, out double x, out double y)
        {
            x = a * b;
            Two_Product_Tail(a, b, x, out y);
        }

        private static void Two_One_Sum(double a1, double a0, double b, out double x2, out double x1, out double x0)
        {
            double _i;
            Two_Sum(a0, b , out _i, out x0);
            Two_Sum(a1, _i, out x2, out x1);
        }

        private static void Two_One_Diff(double a1, double a0, double b, out double x2, out double x1, out double x0)
        {
            double _i;
            Two_Diff(a0, b, out _i, out x0);
            Two_Sum(a1, _i, out x2, out x1);
        }

        private static void Two_Two_Diff(double a1, double a0, double b1, double b0, out double x3, out double x2, out double x1, out double x0)
        {
            double _j, _0;
            Two_One_Diff(a1, a0, b0, out _j, out _0, out x0); 
            Two_One_Diff(_j, _0, b1, out x3, out x2, out x1);
        }


        //functions in predicates.c
        //could probably rewrite this to use with spans more efficiently
        //for now, I am just going to do a direct shewchuck rewrite
        //there may also be other c# ways to make this faster outside of direct c style rewrite
        private static int fast_expansion_sum_zeroelim(int elen, double[] e, int flen, double[] f, double[] h) /* h cannot be e or f. */
        {
            double Q;
            double Qnew;
            double hh;
            //double bvirt;                     --macro constant now
            //double avirt, bround, around;     --macro constant now
            int eindex, findex, hindex;
            double enow, fnow;

            enow = e[0];
            fnow = f[0];
            eindex = findex = 0;
            //original reads 1 past, instead we inc index and read after guards
            if ((fnow > enow) == (fnow > -enow)) 
            {
                Q = enow;
                ++eindex;
            }
            else
            {
                Q = fnow;
                ++findex;
            }

            hindex = 0;
            if (eindex < elen && findex < flen)
            {
                enow = e[eindex];
                fnow = f[findex];
                if ((fnow > enow) == (fnow > -enow)) 
                { 
                    Fast_Two_Sum(enow, Q, out Qnew, out hh); 
                    eindex++;
                    }
                else                       
                { 
                    Fast_Two_Sum(fnow, Q, out Qnew, out hh); 
                    findex++;
                }
                Q = Qnew;
                if (hh != 0.0) 
                {
                    h[hindex++] = hh;
                }

                while (eindex < elen && findex < flen) //original reads 1 past end
                {
                    enow = e[eindex];
                    fnow = f[findex];
                    if ((fnow > enow) == (fnow > -enow)) 
                    { 
                        Two_Sum(Q, enow, out Qnew, out hh); 
                        eindex++; 
                    }
                    else                       
                    { 
                        Two_Sum(Q, fnow, out Qnew, out hh); 
                        findex++; 
                    }
                    Q = Qnew;
                    if (hh != 0.0) {h[hindex++] = hh;}
                }
            }

            //we reread one extra time, shouldnt matter in the long run
            while (eindex < elen)
            {
                enow = e[eindex++];
                Two_Sum(Q, enow, out Qnew, out hh);
                Q = Qnew;
                if (hh != 0.0) {h[hindex++] = hh;}
            }
            while (findex < flen)
            {
                fnow = f[findex++];
                Two_Sum(Q, fnow, out Qnew, out hh);
                Q = Qnew;
                if (hh != 0.0) {h[hindex++] = hh;}
            }

            if (Q != 0.0 || hindex == 0) {h[hindex++] = Q;}
            return hindex;
        }


        private static double estimate(int elen, double[] e)
        {
            double Q;
            int eindex;

            Q = e[0];
            for (eindex = 1; eindex < elen; eindex++) {
                Q += e[eindex];
            }
            return Q;
        }
        

        //only goes as far as needed!
        //double[] pa, double[] pb, double[] pc -> Vector2 pa, Vector2 pb, Vector2 pc avoids allocs
        //THANK YOU JONATHAN SHEWCHUCK
        private static double orient2dadapt(Vector2 pa, Vector2 pb, Vector2 pc, double detsum)
        {
            double acx, acy, bcx, bcy;
            double acxtail, acytail, bcxtail, bcytail;
            double detleft, detright;
            double detlefttail, detrighttail;
            double det, errbound;
            double[] B = new double[4];
            double[] C1 = new double[8];
            double[] C2 = new double[12];
            double[] D = new double[16];
            double B3;
            int C1length, C2length, Dlength;
            double[] u = new double[4];
            double u3;
            double s1, t1;
            double s0, t0;

            /* MACRO CONSTANTS NOW
            double bvirt;
            double avirt, bround, around;
            double c;
            double abig;
            double ahi, alo, bhi, blo;
            double err1, err2, err3;
            double _i, _j;
            double _0;
            */
            
            acx = (double)pa.x - pc.x;
            bcx = (double)pb.x - pc.x;
            acy = (double)pa.y - pc.y;
            bcy = (double)pb.y - pc.y;

            Two_Product(acx, bcy, out detleft,  out detlefttail);
            Two_Product(acy, bcx, out detright, out detrighttail);

            Two_Two_Diff(detleft, detlefttail, detright, detrighttail,
                        out B3, out B[2], out B[1], out B[0]);
            B[3] = B3;

            det = estimate(4, B);
            errbound = ccwerrboundB * detsum;
            if ((det >= errbound) || (-det >= errbound)) {
                return det;
            }

            Two_Diff_Tail(pa.x, pc.x, acx, out acxtail);
            Two_Diff_Tail(pb.x, pc.x, bcx, out bcxtail);
            Two_Diff_Tail(pa.y, pc.y, acy, out acytail);
            Two_Diff_Tail(pb.y, pc.y, bcy, out bcytail);

            if ((acxtail == 0.0) && (acytail == 0.0)
                && (bcxtail == 0.0) && (bcytail == 0.0)) {
                return det;
            }

            errbound = ccwerrboundC * detsum + resulterrbound * Absolute(det);
            det += (acx * bcytail + bcy * acxtail)
                - (acy * bcxtail + bcx * acytail);
            if ((det >= errbound) || (-det >= errbound)) {
                return det;
            }

            Two_Product(acxtail, bcy, out s1, out s0);
            Two_Product(acytail, bcx, out t1, out t0);
            Two_Two_Diff(s1, s0, t1, t0, out u3, out u[2], out u[1], out u[0]);
            u[3] = u3;
            C1length = fast_expansion_sum_zeroelim(4, B, 4, u, C1);

            Two_Product(acx, bcytail, out s1, out s0);
            Two_Product(acy, bcxtail, out t1, out t0);
            Two_Two_Diff(s1, s0, t1, t0, out u3, out u[2], out u[1], out u[0]);
            u[3] = u3;
            C2length = fast_expansion_sum_zeroelim(C1length, C1, 4, u, C2);

            Two_Product(acxtail, bcytail, out s1, out s0);
            Two_Product(acytail, bcxtail, out t1, out t0);
            Two_Two_Diff(s1, s0, t1, t0, out u3, out u[2], out u[1], out u[0]);
            u[3] = u3;
            Dlength = fast_expansion_sum_zeroelim(C2length, C2, 4, u, D);

            return(D[Dlength - 1]);
        }

        //Vector2 of floates for 3 points
        //usually the algorithm assumes double[2], so we must cast at certain points
        //double[] pa, double[] pb, double[] pc -> Vector2 pa, Vector2 pb, Vector2 pc avoids allocs
        public static double orient2d(Vector2 pa, Vector2 pb, Vector2 pc)
        {
            double detleft, detright, det;
            double detsum, errbound;

            detleft = ((double)pa.x - pc.x) * ((double)pb.y - pc.y);
            detright = ((double)pa.y - pc.y) * ((double)pb.x - pc.x);
            det = detleft - detright;

            if (detleft > 0.0) 
            {
                if (detright <= 0.0) {return det;} 
                else {detsum = detleft + detright;}
            } 

            else if (detleft < 0.0) 
            {
                if (detright >= 0.0) {return det;} 
                else {detsum = -detleft - detright;}
            } 

            else {return det;}

            errbound = ccwerrboundA * detsum;
            if ((det >= errbound) || (-det >= errbound)) {return det;}

            return orient2dadapt(pa, pb, pc, detsum);
        }

        //helper function for SiteEvent, basically calls the adaptive orient2d, but always
        //goes to the most exact version (which actually outputs a usable value)
        //returns a usable robust determinant for circumcircle, accurate to 1ulp
        public static double orient2d_exact_determinant(Vector2 pa, Vector2 pb, Vector2 pc)
        {
            double acx, acy, bcx, bcy;
            double acxtail, acytail, bcxtail, bcytail;
            double detleft, detright;
            double detlefttail, detrighttail;
            double[] B = new double[4];
            double[] C1 = new double[8];
            double[] C2 = new double[12];
            double[] D = new double[16];
            double B3;
            int C1length, C2length, Dlength;
            double[] u = new double[4];
            double u3;
            double s1, t1;
            double s0, t0;

            acx = (double)pa.x - pc.x;
            bcx = (double)pb.x - pc.x;
            acy = (double)pa.y - pc.y;
            bcy = (double)pb.y - pc.y;

            Two_Product(acx, bcy, out detleft,  out detlefttail);
            Two_Product(acy, bcx, out detright, out detrighttail);

            Two_Two_Diff(detleft, detlefttail, detright, detrighttail,
                        out B3, out B[2], out B[1], out B[0]);
            B[3] = B3;

            Two_Diff_Tail(pa.x, pc.x, acx, out acxtail);
            Two_Diff_Tail(pb.x, pc.x, bcx, out bcxtail);
            Two_Diff_Tail(pa.y, pc.y, acy, out acytail);
            Two_Diff_Tail(pb.y, pc.y, bcy, out bcytail);

            //B is already the exact determinant
            if ((acxtail == 0.0) && (acytail == 0.0)
                && (bcxtail == 0.0) && (bcytail == 0.0)) {
                return estimate(4, B);
            }
            
            Two_Product(acxtail, bcy, out s1, out s0);
            Two_Product(acytail, bcx, out t1, out t0);
            Two_Two_Diff(s1, s0, t1, t0, out u3, out u[2], out u[1], out u[0]);
            u[3] = u3;
            C1length = fast_expansion_sum_zeroelim(4, B, 4, u, C1);

            Two_Product(acx, bcytail, out s1, out s0);
            Two_Product(acy, bcxtail, out t1, out t0);
            Two_Two_Diff(s1, s0, t1, t0, out u3, out u[2], out u[1], out u[0]);
            u[3] = u3;
            C2length = fast_expansion_sum_zeroelim(C1length, C1, 4, u, C2);

            Two_Product(acxtail, bcytail, out s1, out s0);
            Two_Product(acytail, bcxtail, out t1, out t0);
            Two_Two_Diff(s1, s0, t1, t0, out u3, out u[2], out u[1], out u[0]);
            u[3] = u3;
            Dlength = fast_expansion_sum_zeroelim(C2length, C2, 4, u, D);

            return estimate(Dlength, D);
        }
    }
}
