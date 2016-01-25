using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace CRMSLASystem
{
    /// <summary>
    /// Abstraction of a CRM Business Closure.
    /// </summary>
    public class CRMBusinessClosure
    {
        private IOrganizationService _service;
        private IWorkflowContext _context;

        /// <summary>
        /// Main constructor for CRMBusinessClosure.
        /// </summary>
        /// <param name="service">Workflow execution service as an IOrganizationService..</param>
        /// <param name="context">Workflow execution context as an IWorkflowContext.</param>
        public CRMBusinessClosure(IOrganizationService service, IWorkflowContext context)
        {
            _service = service;
            _context = context;
        }

        /// <summary>
        /// Gets CRM Business Closures.
        /// </summary>
        /// <returns>Returns an IList of retrieved CRM Business Closure entity records.</returns>
        private IList<Entity> GetBusinessClosureCalendarRules()
        {
            // Retrieve Organisation Business Closure Calendar Id.
            Entity organisation = _service.Retrieve("organization", _context.OrganizationId, new ColumnSet("businessclosurecalendarid"));

            QueryExpression calendarQuery = new QueryExpression("calendar");
            calendarQuery.ColumnSet = new ColumnSet(true);
            calendarQuery.Criteria = new FilterExpression();
            calendarQuery.Criteria.AddCondition(new ConditionExpression("calendarid", ConditionOperator.Equal, organisation["businessclosurecalendarid"].ToString()));

            Entity businessClosureCalendar = _service.RetrieveMultiple(calendarQuery).Entities[0];
            if (businessClosureCalendar != null)
            {
                return businessClosureCalendar.GetAttributeValue<EntityCollection>("calendarrules").Entities.ToList<Entity>();
            }
            return null;
        }

        /// <summary>
        /// Gets relevant CRM Business Closures specified within two DateTime boundaries.
        /// </summary>
        /// <param name="startDate">The start date from which a CRM Business Closure is considered valid.</param>
        /// <param name="endDate">The end date before which a CRM Business Closure is considered valid.</param>
        /// <returns>Returns an IList of CRM Business Closures valid within two DateTime boundaries.</returns>
        public IList<Entity> GetBusinessClosureCalendarRules(DateTime startDate, DateTime endDate)
        {
            IList<Entity> businessClosures = GetBusinessClosureCalendarRules();

            var validBusinessClosures = from a in businessClosures
                                        where ((DateTime)a["starttime"] >= startDate
                                        && (DateTime)a["starttime"] <= endDate)
                                        || ((((DateTime)a["starttime"]).AddMinutes((int)a["duration"])) >= startDate
                                        && (((DateTime)a["starttime"]).AddMinutes((int)a["duration"])) <= endDate)
                                        || ((((DateTime)a["starttime"]).AddMinutes((int)a["duration"])) > endDate
                                        && (DateTime)a["starttime"] < startDate)
                                        select a;

            IList<Entity> validClosures = new List<Entity>();

            foreach (var validBusinessClosure in validBusinessClosures)
            {
                validClosures.Add((Entity)validBusinessClosure);
            }

            return validClosures;
        }

        /// <summary>
        /// For a CRM Business closure, gets the time worked during working hours excluding time
        /// invalidated by the Business Closure.
        /// </summary>
        /// <param name="businessOpen">The DateTime business opens.</param>
        /// <param name="businessClose">The DateTime business closes.</param>
        /// <param name="closureStart">When the CRM Business Closure begins as a DateTime.</param>
        /// <param name="closureEnd">When the CRM Business Closure ends as a DateTime.</param>
        /// <returns>Returns the amount of time that would have been worked in a CRM Business Closure as an int[3]
        /// where int[ { days, hours, minutes}]. </returns>
        public static int[] GetTimeWorked(DateTime businessOpen, DateTime businessClose, DateTime closureStart, DateTime closureEnd)
        {
            if (businessOpen > closureEnd || businessClose < closureStart)
                return new int[] { 0, 0, 0 };

            if (businessOpen > closureStart && businessClose < closureEnd)
                return new int[] { 1, 0, 0 };

            DateTime currentTime = businessOpen > closureStart ? businessOpen : closureStart;
            DateTime currentEnd = businessClose < closureEnd ? businessClose : closureEnd;
            TimeSpan duration = currentEnd - currentTime;

            return new int[] { 0, duration.Hours, duration.Minutes };
        }   
    }
}
