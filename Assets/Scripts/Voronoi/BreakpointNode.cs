using System;
using UnityEngine;


//BreakpointNodes are stored in the internal nodes of the BST
public class BreakpointNode : INode
{   
    //private const double tolerance = 1e-9; decided not to use for now
    public INode Left { get; set; }
    public INode Right { get; set; }
    public INode Parent { get; set; }
    public int Height { get; set; } = 0;


    //<p_i, p_j>, where p_i is the arc to the left
    //            and p_j is the arc to the right
    public (Vector2 p_i, Vector2 p_j) breakpoint;
    //HalfEdge being traced by breakpoint
    public HalfEdge halfEdge { get; set; }

    public BreakpointNode(Vector2 p_i, Vector2 p_j)
    {
        breakpoint = (p_i, p_j);
    }


    //large comment explaining logic of calculate breakpoint at the bottom of the page
    //TLDR: Calculate (VertJ*xpi - VertI*xpj) + sqrt(VertI*VertJ(|pi - pj|^2)) / (VertJ - VertI)
    public double CalculateBreakpoint(double directrix)
    {
        //use doubles for precision
        double VertI = breakpoint.p_i.y - directrix;
        double VertJ = breakpoint.p_j.y - directrix;
        //Degenerate case where both sites are on sweepline OR
        //both sites are on the same horizontal axis
        //return midpoint
        if(VertI == VertJ){ //|| Math.Abs(VertI - VertJ) < tolerance - decided against for now
            return (breakpoint.p_i.x + breakpoint.p_j.x) / 2;
        }

        double distanceSq = (breakpoint.p_i.x-breakpoint.p_j.x)*(breakpoint.p_i.x-breakpoint.p_j.x) +
                            (breakpoint.p_i.y-breakpoint.p_j.y)*(breakpoint.p_i.y-breakpoint.p_j.y);
        //Max is just there because I think floating point error has the chance of pushing the Sqrt negative
        //Alternatively we could check variables against tolerance

        return ((VertJ * breakpoint.p_i.x - VertI * breakpoint.p_j.x) 
             + (Math.Sqrt(Math.Max(0,VertI*VertJ*distanceSq))))
             / (VertJ - VertI);
        //For anyone reading: If it gets too confusing to keep track of later (if I need to come back to this function),
        //I will probably separate these variables to make it easier to debug, for now this is good 
    }
}

    /*
        CalculateBreakpoint derivation and logic:

        the sweepline's y coordinate acts as our directrix
        to figure out what terms we actually need to solve with and which cancel,
        (less terms = less work + less chance of floating point error),
        we derive the following (there may be a better solution to be honest!): 

        solving y = ((x - xp)^2 + yp^2 - dir^2) / (2 * (yp - dir)) for each parabola,
        y_i = y_j => 
              ((x - xpi)^2 + ypi^2 - dir^2) / (2 * (ypi - dir)) =
              ((x - xpj)^2 + ypj^2 - dir^2) / (2 * (ypj - dir))

        VertI = ypi - dir, VertJ = ypj - dir => vertical distances from pi/j to directrix

        convert to terms, cross-mult, expand, then solve with standard quadratic formula:
        VertJ((x - xpi)^2 + ypi^2 - dir^2) = VertI((x - xpj)^2 + ypj^2 - dir^2)
        VertJ((x^2 + xpi^2 - 2*x*xpi + ypi^2 - dir^2) = VertI((x^2 + xpj^2 - 2*x*xpj + ypj^2 - dir^2)
        VertJ((x^2 + xpi^2 - 2*x*xpi + ypi^2 - dir^2) - VertI((x^2 + xpj^2 - 2*x*xpj + ypj^2 - dir^2) = 0
        (VertJ - VertI)*x^2 + 2*(VertI*xpj - VertJ*xpi)*x + (VertJ(xpi^2 + ypi^2 - dir^2) - VertI(xpj^2 + ypj^2 - dir^2)) = 0
    
        a = (VertJ - VertI), b = 2*(VertI*xpj - VertJ*xpi), c = VertJ(xpi^2 + ypi^2 - dir^2) - VertI(xpj^2 + ypj^2 - dir^2)
        x = (-b +- sqrt(b^2 - 4ac)) / (2a) =>
          = (-2*(VertI*xpj - VertJ*xpi) +- sqrt((2*(VertI*xpj - VertJ*xpi)^2 - 4*(VertJ - VertI)*(VertJ(xpi^2 + ypi^2 - dir^2) - VertI(xpj^2 + ypj^2 - dir^2))))
                                                  / 2*(VertJ - VertI)

        simplified: - for the following section, I will shorthand I to I and VertJ to J, and xpi, xpj, ypi, and ypj to xi, xj, yi, and yj
        b^2  = 4(Ixj - Jxi)^2 = 4I^2xj^2 + 4J^2xi^2 - 8IJxixj
        4ac =  4(-I+J)(Jxi^2 + Jyi^2 - Jdir^2 - Ixj^2 -Iyj^2 + Idir^2)
             = 4(-IJxi^2 -IJyi^2 + IJdir^2 + I^2xj^2 + I^2yj^2 - I^2dir^2 + J^2xi^2 + J^2yi^2 - J^2dir^2 - IJxj^2 -IJyj^2 + IJdir^2)
             = 4(-IJxi^2 -IJyi^2 + 2IJdir^2 + I^2xj^2 + I^2yj^2 - I^2dir^2 + J^2xi^2 + J^2yi^2 - J^2dir^2 - IJxj^2 - IJyj^2)

  group desc : |   dir terms    |  xj^2 & xi^2 terms  |  yi^2 & yj^2 terms  |
             = -4*dir^2*(I-J)^2 + 4(I-J)(Ixj^2-Jxi^2) + 4(I-J)(Iyj^2-Jyi^2)       -- group like terms

                            [I=yi-dir | J=yj-dir | I-J = yi-yj]
             = -4*dir^2*(yi-yj)^2 + 4(yi-yj)(Ixj^2-Jxi^2) + 4(yi-yj)(Iyj^2-Jyi^2)           -- sub I-J
             = -4*dir^2*(yi-yj)^2 + 4(yi-yj)(I(xj^2 + yj^2) - J(xi^2+yi^2))                 -- group like terms
             = 4(yi-yj)(-dir^2*(yi-yj) + I(xj^2 + yj^2) - J(xi^2+yi^2))                     -- factor 4(yi-yj)
             = 4(yi-yj)(-dir^2*(yi-yj) + I(xj^2)+(yi-dir)(yj^2) - J(xi^2)-(yj-dir)(yi^2))   -- sub I and J on y components
             = 4(yi-yj)(-dir^2*(yi-yj) + I(xj^2)-J(xi^2) + yiyj^2-yjyi^2 -diryj^2+diryi^2   
             = 4(yi-yj)(-dir^2*(yi-yj) + I(xj^2)-J(xi^2) + (yi-yj)(-yiyj + dir(yi+yj))      -- factor (yi-yj)
             = 4(yi-yj)(I(xj^2)-J(xi^2) + (yi-yj)(-yiyj + dir(yi+yj) - dir^2))              -- group line terms
             = 4(yi-yj)(I(xj^2)-J(xi^2) + (yi-yj)(-(yi-dir)(yj-dir)))                       -- factor
             = 4(yi-yj)(Ixj^2 - Jxi^2 + (yi-yj)(-(I)(J)))                                   -- sub in I and J
             = 4(yi-yj)(Ixj^2 - Jxi^2  - IJ(yi-yj))
             = 4(yi-yj)(Ixj^2 - Jxi^2) - 4IJ(yi-yj)^2
             = 4(I-J)(Ixj^2 - Jxi^2) - 4IJ(yi-yj)^2                                         -- sub in I and J 
             = 4I^2xj^2 + 4J^2xi^2 - 4IJxj^2 - 4IJxi^2 - 4IJ(yi-yj)^2                       -- expand

    b^2-4ac  = 4I^2xj^2 + 4J^2xi^2 - 8IJxixj - 4I^2xj^2 - 4J^2xi^2 + 4IJxj^2 + 4IJxi^2 + 4IJ(yi-yj)^2
             = -8IJxixj + 4IJxj^2 + 4IJxi^2 + 4IJ(yi-yj)^2                                  -- cancel out terms
             = 4IJ(-2xixj + xj^2 + xi^2 + (yi-yj)^2)
             = 4IJ((xi - xj)^2 + (yi-yj)^2)                                                 -- factor
             = 4IJ(|pi - pj|^2)                                                             -- put in terms of distance formula
    

            
           x = -2*(VertI*xpj - VertJ*xpi) +- sqrt(4*VertI*VertJ(|pi - pj|^2)) / 2*(VertJ - VertI)
             = -2*(VertI*xpj - VertJ*xpi) +- 2*sqrt(VertI*VertJ(|pi - pj|^2)) / 2*(VertJ - VertI)
             = (VertJ*xpi - VertI*xpj) +- sqrt(VertI*VertJ(|pi - pj|^2)) / (VertJ - VertI) 

             when ypi > ypj, VertI > VertJ   -- (VertJ - VertI) < 0
             when ypi < ypj, VertI < VertJ   -- (VertJ - VertI) > 0

             usually, when ypi > ypj, you take the negative root  (left intersection)
                  and when ypi < ypj, you take the positive root  (right intersection)
             this is because lower sites "split" arcs from higher sites
             left intersection minimizes x, right intersection maximizes x

             but because the denominator switches signs when ypi > ypj, we always take the pos root
             using : (VertJ*xpi - VertI*xpj) + sqrt(VertI*VertJ(|pi - pj|^2)) / (VertJ - VertI)
            

             proof : f(x) = (VertJ-VertI)*x^2 + 2*(VertI*xpj-VertJ*xpi)*x + c = 2*VertI*VertJ*(yi(x)-yj(x))
    f(x) = 0 => yi(x) = yj(x) | f(x) > 0 => yi(x) > yj(x) | f(x) < 0 => yi(x) < yj(x)
    f(x) = 0 => yi(x) = yj(x) | f(x) > 0 => beachline = yj(x) | f(x) < 0 => beachline = yi(x)
                    
                    f'(x) = 2*(VertJ-VertI)*x + 2*(VertI*xpj-VertJ*xpi)    -- at breakpoint x, the sign shows which way we are crossing 
    f'(x) > 0 => yi(x) is increasing faster than yj(x) at breakpoint, and thus yj(x) is on the beachline after breakpoint
    f'(x) < 0 => yj(x) is increasing faster than yi(x) at breakpoint, and thus yi(x) is on the beachline after breakpoint
    f'(x) = 0 => degenerate case, where either pi, pj sits on the beachline, or pi and pj are the same point

    because we know yi(x) forms the arc to the left of the breakpoint on the beachline, and yj(x) the right,
    we know f'(x) > 0 at our breakpoint's x coordinate.

    p = (VertJ*xpi - VertI*xpj) + sqrt(VertI*VertJ(|pi - pj|^2)) / (VertJ - VertI)
    n = (VertJ*xpi - VertI*xpj) - sqrt(VertI*VertJ(|pi - pj|^2)) / (VertJ - VertI)

    f'(p) = 2*(VertJ-VertI)*p + 2*(VertI*xpj-VertJ*xpi) 
          = 2*(VertJ-VertI) * ((VertJ*xpi - VertI*xpj) + sqrt(VertI*VertJ(|pi - pj|^2))) / (VertJ - VertI) + 2*(VertI*xpj-VertJ*xpi)
          = 2*(VertJ*xpi - VertI*xpj) + 2*sqrt(VertI*VertJ(|pi - pj|^2)) + 2*(VertI*xpj-VertJ*xpi)
          = 2*sqrt(VertI*VertJ(|pi - pj|^2))

    f'(n) = 2*(VertJ-VertI)*n + 2*(VertI*xpj-VertJ*xpi) 
          = 2*(VertJ-VertI) * ((VertJ*xpi - VertI*xpj) - sqrt(VertI*VertJ(|pi - pj|^2))) / (VertJ - VertI) + 2*(VertI*xpj-VertJ*xpi)
          = 2*(VertJ*xpi - VertI*xpj) - 2*sqrt(VertI*VertJ(|pi - pj|^2)) + 2*(VertI*xpj-VertJ*xpi)
          = -2*sqrt(VertI*VertJ(|pi - pj|^2))

    f'(p) ALWAYS > 0, and f'(n) ALWAYS < 0 for any pi, pj, directrix combination
    because of this, we always take the positive root when finding our intersection, as the positive root
    ALWAYS describes the intersection where the arc of pi is to the left of pj

    the negative root describes the other intersection, where pj is to the left of pi

    Thus, for our program, we simply calculate (VertJ*xpi - VertI*xpj) + sqrt(VertI*VertJ(|pi - pj|^2)) / (VertJ - VertI)
    we only worry about degenerate cases conditionally and can avoid intermediate calculations
        */   