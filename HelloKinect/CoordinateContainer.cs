﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloKinect
{
    public class CoordinateContainer
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
            return String.Format("[{0}: {1}]", x_position.ToString(), y_position.ToString());
        }
    }

    public class GesturePackage{
        String gesture_name;
        List<CoordinateContainer> left_coordinates_list = new List<CoordinateContainer>();
        List<CoordinateContainer> right_coordinates_list = new List<CoordinateContainer>();

        public GesturePackage(String name) {
            gesture_name = name;
        }

        public String getName() {
            return gesture_name;
        }
        public void setLeftCoordinates(CoordinateContainer left_coordinate) {
            left_coordinates_list.Add(left_coordinate);
        }

        public void setRightCoordinates(CoordinateContainer right_coordinate) {
            right_coordinates_list.Add(right_coordinate);
        }

        public List<CoordinateContainer> getLeftCoordinates() {
            return left_coordinates_list;
        }

        public List<CoordinateContainer> getRightCoordinates() {
            return right_coordinates_list;
        }

    }
}
