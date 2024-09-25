using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CreatingModel
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level level1 = GetLevel("Уровень 1", doc);
            Level level2 = GetLevel("Уровень 2", doc);
           List<Wall> walls= ConstructionWalls(10000, 5000, level1, level2, doc);
            AddDoor(doc, walls[0], level1);
            AddWindow(doc, walls[1], level1);
            AddWindow(doc, walls[2], level1);
            AddWindow(doc, walls[3], level1);

            return Result.Succeeded;
        }

        public static Level GetLevel(string levelName, Document doc)
        {
            var listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level level = listLevel
                .Where(x => x.Name.Equals(levelName))
                .FirstOrDefault();
            return level;
        }

        public static List<Wall> ConstructionWalls(double _width, double _depth,
            Level lowerLevel, Level upperLevel, Document doc)
        {
            double width = UnitUtils.ConvertToInternalUnits(_width, UnitTypeId.Milliamperes);
            double depth = UnitUtils.ConvertToInternalUnits(_depth, UnitTypeId.Milliamperes);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction ts = new Transaction(doc, "Создание стен");
            ts.Start();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, lowerLevel.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(upperLevel.Id);

            }
            ts.Commit();
            return walls;
        }

        public static void AddDoor(Document doc, Wall wall, Level level)
        {
            var doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0762 x 2032 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point= (point1 + point2)/2;


            Transaction ts = new Transaction(doc,"Добавление двири") ;
            ts.Start();
            if(!doorType.IsActive)
                doorType.Activate();
            doc.Create.NewFamilyInstance(point, doorType, wall, level, StructuralType.NonStructural);
            ts.Commit();
        }


        public static void AddWindow(Document doc, Wall wall, Level level)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0610 x 0610 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            double wallHeight = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            point1  = point1 +  new XYZ ( 0,0, wallHeight / 2);

            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            point2 = point2 + new XYZ(0, 0, wallHeight/2);

           XYZ point = (point1 + point2) / 2;

            Transaction ts = new Transaction(doc, "Добавление окна");
            ts.Start();
            if (!windowType.IsActive)
                windowType.Activate();
            doc.Create.NewFamilyInstance(point, windowType, wall, StructuralType.NonStructural);
            ts.Commit();
        }




    }
}
