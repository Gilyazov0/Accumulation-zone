using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace Accumulation_zone
{
    public enum AccumulationZoneBorderType
    {
        [LocalizeDescription("Средняя цена кластера")] AveragePrice,
        [LocalizeDescription("Максимальная/минимальная граница кластера")] Max_MinPrice,
        [LocalizeDescription("Максимальная/минимальная цена инструмента в диапазоне кластера")] Hight_LowPrice
    }

    [HandlerCategory("S.Gilyazov")]
    [HelperName("Accumulation zone", Language = "en-US")]
    [HelperName("Зона накопления", Language = "ru-RU")]
    [InputsCount(2)]
    [Input(0, TemplateTypes.TRADE_STATISTICS, Name = "SECURITYSource")]
    [Input(1, TemplateTypes.DOUBLE, Name = "размер ЗН")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE2)]
    [Description("Зона накопления")]
    [HelperDescription("Accumulation zone", "en-US")]
    public class Accumulation_zone : IStreamHandler, IDoubleReturns, IContextUses
    { 
        /// <summary>
        /// \~english Accumulation zone border type 
        /// \~russian Тип границы зоны накопления
        /// </summary>
        [HelperName("Border type", "en-us")]
        [HelperName("Тип границы", "ru-ru")]
        [Description("Тип границы зоны накопления")]
        [HelperDescription("Accumulation zone border type", "en-us")]
        [HandlerParameter(true, "Hight_LowPrice")]
        public AccumulationZoneBorderType BorderType { get; set; }
        
        public IContext Context { get; set; }
        
        public IList<Double2> Execute(ITradeStatisticsWithKind tradeStat, IList<double> size)
        {
            if (size[0] < 0 || size[0] > 100)
                throw new InvalidOperationException(
                    $"Задан некорректный размер зоны накопления:{size[0]}. Значение должно быть больше 0 и меньше 100");

            var count = Context.BarsCount;
            var borders = Context.GetArray<Double2>(count);
            var d2 = new Double2();
            var tsProvider = tradeStat.CreateAggregatedHistogramBarsProvider();
          
            for (int i = 0; i < count; i++)
            {
                var bar = tsProvider.GetAggregatedHistogramBars(i);

                Context.Log($"bar.Count = {bar.Count}");
                if (bar.Count == 0)
                {
                    d2.V1 = 0;
                    d2.V2 = 0;
                    borders[i] = (d2);
                    Context.Log($"Обнаружен пустой бар торговой статистики.",MessageType.Warning,true);
                    continue;
                }
                bar = bar.OrderBy(x => x.AveragePrice).ToList();

                var currentQuantity = 0.0;
                var upNumber = 0;
                var downNumber = 0;
                var totalQuantity = 0.0;

                for (int j = 0; j < bar.Count; j++)
                {
                    if (currentQuantity < bar[j].Quantity)
                    {
                        currentQuantity = bar[j].Quantity;
                        downNumber = j;                       
                    }

                    totalQuantity += bar[j].Quantity;
                }
                upNumber = downNumber;

                while ((currentQuantity / totalQuantity) * 100 < size[0])
                {
                    var upQ = -1.0;
                    var downQ = -1.0;
                    upQ = upNumber < bar.Count - 1 ? bar[upNumber + 1].Quantity : 0;
                    downQ = downNumber > 0 ? bar[downNumber - 1].Quantity : 0;

                    if (upQ > downQ)
                    {
                        currentQuantity += upQ;
                        upNumber++;
                    }
                    else if (downQ > 0 || downNumber > 0)
                    {
                        currentQuantity += downQ;
                        downNumber--;
                    }
                    else
                    {
                        currentQuantity += upQ;
                        upNumber++;
                    }
                }

                switch (BorderType)
                {
                    case AccumulationZoneBorderType.AveragePrice:
                        d2.V1 = bar[upNumber].AveragePrice;
                        d2.V2 = bar[downNumber].AveragePrice;
                        break;
                    case AccumulationZoneBorderType.Hight_LowPrice:
                        d2.V1 = bar[upNumber].HighPrice;
                        d2.V2 = bar[downNumber].LowPrice;
                        break;
                    case AccumulationZoneBorderType.Max_MinPrice:
                        d2.V1 = bar[upNumber].MaxPrice;
                        d2.V2 = bar[downNumber].MinPrice;
                        break;
                    default:
                        throw new InvalidEnumArgumentException("Accumulation zone", (int)this.BorderType, this.BorderType.GetType());
                }
                 
                borders[i] = (d2);
            }

            return borders;
        }
    }

    [HandlerCategory("S.Gilyazov")]
    [HelperName("Верхняя граница зоны накопления", Language = "en-US")]
    [HelperName("Upper border", Language = "ru-RU")]
    [InputsCount(1)]
    [Input(0, TemplateTypes.DOUBLE2, Name = "Зона накопления")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Верхняя граница зоны накопления")]
    [HelperDescription("Верхняя граница зоны накопления", "en-US")]
    public class UpperBorder : IStreamHandler, IDoubleReturns, IContextUses
    {
        public IContext Context { get; set; }

        public IList<double> Execute(IList<Double2> data)
        {
            var count = Context.BarsCount;
            var upBorder = Context.GetArray<double>(count);

            for (int i = 0; i < count; i++)
            {
                upBorder[i] = data[i].V1;
            }

            return upBorder;
        }
    }

    [HandlerCategory("S.Gilyazov")]
    [HelperName("Нижняя граница зоны накопления", Language = "en-US")]
    [HelperName("Lower border", Language = "ru-RU")]
    [InputsCount(1)]
    [Input(0, TemplateTypes.DOUBLE2, Name = "Зона накопления")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Нижняя граница зоны накопления")]
    [HelperDescription("Нижняя граница зоны накопления", "en-US")]
    public class LowerBorder : IStreamHandler, IDoubleReturns, IContextUses
    {
        public IContext Context { get; set; }

        public IList<double> Execute(IList<Double2> data)
        {
            var count = Context.BarsCount;
            var upBorder = Context.GetArray<double>(count);

            for (int i = 0; i < count; i++)
            {
                upBorder[i] = data[i].V2;
            }

            return upBorder;
        }
    }
}
