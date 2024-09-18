using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
            ConstructionWalls(10000,5000,
                GetLevel("Уровень 1",doc),
                GetLevel("Уровень 2", doc),
                doc);

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

        public static void ConstructionWalls(double _width, double _depth,
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

        }
    }
}
