using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloKinect
{
    class CoordinateContainer
    {
        protected double x_position;
        protected double y_position;
        public CoordinateContainer(double x, double y) {
            x_position = x;
            y_position = y;
        }

        public double getX() {
            return x_position;
        }

        public double getY() {
            return y_position;
        }

        public override String ToString(){
            return String.Format("[{0}, {1}]", x_position.ToString(), y_position.ToString());
        }
    }
}
