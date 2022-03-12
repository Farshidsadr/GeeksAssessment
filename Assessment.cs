using System;
using System.Collections.Generic;
using System.Linq;

public class Assessment : IAssessment
{
    /// <summary>
    /// Returns the score with the highest value
    /// </summary>
    public Score? WithMax(IEnumerable<Score> scores)
    {
        if (scores == null || !scores.Any())
            return null;
        return scores.Aggregate((i1, i2) => i1.Value > i2.Value ? i1 : i2);
    }

    /// <summary>
    /// Returns the average value of the collection. For an empty collection it returns null
    /// </summary>
    public double? GetAverageOrDefault(IEnumerable<int> items)
    {
        if (!items.Any())
            return null;

        return items.Average();
    }


    /// <summary>
    /// Appends the suffix value to the text if the text has value. If not, it returns empty.
    /// </summary>
    public string WithSuffix(string text, string suffixValue)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text + suffixValue;
    }

    /// <summary>
    /// It fetches all the data from the source.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public IEnumerable<Score> GetAllScoresFrom(IDataProvider<Score> source)
    {
        var tempToken = string.Empty;
        return source.GetData(tempToken).Items;
    }

    /// <summary>
    /// Returns child's name prefixed with all its parents' names separated by the specified separator.Example : Parent/Child
    /// </summary>
    public string GetFullName(IHierarchy child, string separator = null)
    {
        var pointer = child;
        LinkedList<string> result = new LinkedList<string>();
        separator ??= "/";

        while (pointer.Parent != null)
        {
            result.AddFirst(pointer.Name);
            pointer = pointer.Parent;
        }
        result.AddFirst(pointer.Name);

        return string.Join(separator, result.AsEnumerable());
    }

    /// <summary>
    /// Refactor: Returns the value that is closest to the average value of the specified numbers.
    /// </summary>
    public int? ClosestToAverageOrDefault(IEnumerable<int> numbers)
    {
        int closestNumbr = numbers.First();
        double minDifference = int.MaxValue;
        double average = numbers.Average();

        foreach (var number in numbers)
        {
            var difference = Math.Abs(number - average);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestNumbr = number;
            }
        }

        return closestNumbr;
    }

    /// <summary>
    /// Groups the specified bookings based on their consecutive dates then by their projects and finally the booking allocation. Read the example carefully.
    /// Example : [{Project:HR, Date: 01/02/2020 , Allocation: 10},
    ///            {Project:CRM, Date: 01/02/2020 , Allocation: 15},
    ///            {Project:HR, Date: 02/02/2020 , Allocation: 10},
    ///            {Project:CRM, Date: 02/02/2020 , Allocation: 15},
    ///            {Project:HR, Date: 03/02/2020 , Allocation: 15},
    ///            {Project:CRM, Date: 03/02/2020 , Allocation: 15},
    ///            {Project:HR, Date: 04/02/2020 , Allocation: 15},
    ///            {Project:CRM, Date: 04/02/2020 , Allocation: 15},
    ///            {Project:HR, Date: 05/02/2020 , Allocation: 15},
    ///            {Project:CRM, Date: 05/02/2020 , Allocation: 15},
    ///            {Project:ECom, Date: 05/02/2020 , Allocation: 15},
    ///            {Project:ECom, Date: 06/02/2020 , Allocation: 10},
    ///            {Project:CRM, Date: 06/02/2020 , Allocation: 15}
    ///            {Project:ECom, Date: 07/02/2020 , Allocation: 10},
    ///            {Project:CRM, Date: 07/02/2020 , Allocation: 15}]    
    /// Returns : 
    ///          [
    ///            { From:01/02/2020 , To:02/02/2020 , [{ Project:CRM , Allocation:15 },{ Project:HR , Allocation:10 }]  },
    ///            { From:03/02/2020 , To:04/02/2020 , [{ Project:CRM , Allocation:15 },{ Project:HR , Allocation:15 }]  },
    ///            { From:05/02/2020 , To:05/02/2020 , [{ Project:CRM , Allocation:15 },{ Project:HR , Allocation:15 },{ Project:ECom , Allocation:15 }]  },
    ///            { From:06/02/2020 , To:07/02/2020 , [{ Project:CRM , Allocation:15 },{ Project:ECom , Allocation:10 }]  }
    ///          ]
    /// </summary>

    #region Models
    private class GroupedBookingByDate
    {
        public DateTime DateTime { get; set; }
        public List<Booking> Bookings { get; set; } = new List<Booking>();
    }

    private class GroupedBookingByDatePeriod
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<Booking> Bookings { get; set; } = new List<Booking>();
    }
    #endregion


    public IEnumerable<BookingGrouping> Group(IEnumerable<Booking> dates)
    {
        var result = new List<BookingGrouping>();

        // Group dates by datetime and assigned it to a defined class
        var bookingss = dates.GroupBy(p => p.Date).Select(p => new GroupedBookingByDate()
        {
            DateTime = p.Key,
            Bookings = p.ToList()
        });

        // Seperate grouped items into pairs by datetime sequence
        var aggregatedBookings = AggregateBookings(bookingss.ToList());

        // Group each item in periodic booking list by project and allocation
        foreach (var item in aggregatedBookings)
        {
            var bookingGroupingItems = item.Bookings.GroupBy(p => new { p.Project, p.Allocation })
                .Select(p => new BookingGroupingItem()
                {
                    Project = p.Key.Project,
                    Allocation = p.Key.Allocation
                }).ToList();
            result.Add(new BookingGrouping()
            {
                From = item.From,
                To = item.To,
                Items = bookingGroupingItems
            });
        }

        return result;
    }

    private static List<GroupedBookingByDatePeriod> AggregateBookings(List<GroupedBookingByDate> groupedItemsByDate)
    {
        // I think you have a mistake in output result of group, or documentation is incorrect or incomplete.
        // So i supposed your mean is each two consecutive datetimes should be in a group.
        // And if a single item remains, the datetime period should have the same From and To of that single item.

        var result = new List<GroupedBookingByDatePeriod>();
        for (int i = 0; i < groupedItemsByDate.Count(); i++)
        {
            if (i + 1 < groupedItemsByDate.Count)
            {
                result.Add(new GroupedBookingByDatePeriod()
                {
                    From = groupedItemsByDate[i].DateTime,
                    To = groupedItemsByDate[i + 1].DateTime,
                    Bookings = groupedItemsByDate[0].Bookings.Concat(groupedItemsByDate[1].Bookings).ToList()
                });
                i++;
            }
            else
            {
                result.Add(new GroupedBookingByDatePeriod()
                {
                    From = groupedItemsByDate[i].DateTime,
                    To = groupedItemsByDate[i].DateTime,
                    Bookings = groupedItemsByDate[i].Bookings
                });
            }
        }
        return result;
    }

    /// <summary>
    /// Merges the specified collections so that the n-th element of the second list should appear after the n-th element of the first collection. 
    /// Example : first : 1,3,5, 7, 9 second : 2,4,6 -> result : 1,2,3,4,5,6
    /// </summary>
    public IEnumerable<int> Merge(IEnumerable<int> first, IEnumerable<int> second)
    {
        int[] result = new int[(first.Count() + second.Count())];
        int index = 0, i = 0;

        foreach (var item in first)
        {
            result[index++] = item;

            if (i < second.Count())
            {
                result[index++] = second.ElementAt(i);
            }

            i++;
        }

        while (i < second.Count())
        {
            result[index++] = second.ElementAt(i++);
        }

        return result.AsEnumerable();
    }
}
