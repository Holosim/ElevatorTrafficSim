using System;
using System.Collections.Generic;
using System.IO;

/*
 * Code Explanation
 * In summary, DataLogger provides a lightweight mechanism to collect and persist event messages in the 
 * elevator simulation. It supports console and file logging, buffers entries to avoid frequent I/O, 
 * and offers two ways to write the buffer to a file: 
 *      - Flush (append and clear) 
 *      - SaveReport (overwrite without clearing)
 *      
 *  Purpose: The DataLogger class is a utility for recording simulation events. 
 *           It is designed to log messages both to the console (for immediate feedback) 
 *           and to a file (for later review or analysis), 
 *           without letting I/O operations slow down the simulation.
 *  Fields:
 *      logFilePath - stores the path to the file where log entries should be saved. 
 *                    It is initialised to an empty string if no path is provided,
 *                    effectively disabling file logging.
 *      eventBuffer - is a list of strings holding all logged events in memory until they are flushed. 
 *                    Buffering allows the simulation to defer disk writes and therefore avoids frequent file I/O,
 *                    which could introduce timing artefacts.
 *                    
 *  Constructor: 
 *      The constructor accepts an optional logFilePath. 
 *      If the argument is null, it assigns an empty string to logFilePath, meaning no file output will occur. 
 *      It also instantiates the eventBuffer. This design makes the logger flexible: it can log to the console only,
 *      to a file, or both, depending on the provided path.
 * 
 *  LogEvent method: 
 *      This method formats a log message by prefixing it with a timestamp in HH:mm:ss format. 
 *      It then appends the formatted message to eventBuffer and writes it to Console.WriteLine, 
 *      providing immediate feedback. The method returns void because it only performs side effects.
 *  
 *  Flush method: 
 *      When called, Flush checks whether a log file path was provided and whether there are any buffered entries. 
 *      If both are true, it appends all buffered entries to the file and clears the buffer.
 *      Clearing the buffer ensures that the same entries are not written again on the next flush. 
 *      If no file path is specified or the buffer is empty, Flush does nothing.
 *  
 *  SaveReport method: 
 *      This method writes the entire contents of eventBuffer to the log file in one call to File.WriteAllLines, 
 *      overwriting any existing content. It does not clear the buffer afterwards. 
 *      Because of this, repeatedly calling SaveReport can result in duplicate entries being written to the file.
 *      This method might be used to generate a report at a particular point in time, leaving the buffer untouched 
 *      so that it can still be flushed or saved later.
*/

namespace ElevatorSim.DataIO
{
    /**
     * @brief Simple logger for recording simulation events.
     *
     * A DataLogger records timestamped messages to both the console and an
     * optional log file.  Events are buffered in memory so that writing to
     * disk can be deferred until convenient.  This separation prevents the
     * overhead of file I/O from affecting the simulation’s timing.
     */
public class DataLogger
    {
        /** @brief Path to the log file.  Empty if file logging is disabled. */
        private readonly string logFilePath;

        /** @brief Buffer that holds log entries in memory until flushed. */
        private readonly List<string> eventBuffer;

        /**
         * @brief Constructs a new DataLogger.
         *
         * If @p logFilePath is null or empty, log entries will only be
         * printed to the console and not written to disk.
         *
         * @param logFilePath Optional path to a log file.  Leave null or empty
         *                    to disable file logging.
         */
        public DataLogger(string? logFilePath = null)
        {
            this.logFilePath = logFilePath ?? string.Empty;
            eventBuffer = new List<string>();
        }

        /**
         * @brief Records an event message.
         *
         * The message is prefaced with a timestamp and added to the internal
         * buffer.  It is also immediately written to standard output.
         *
         * @param message The event message to log.
         */
        public void LogEvent(string message)
        {
            string entry = $"{DateTime.Now:HH:mm:ss} - {message}";
            eventBuffer.Add(entry);
            Console.WriteLine(entry);
        }

        /**
         * @brief Flushes buffered log entries to disk.
         *
         * If a non‑empty log file path was provided at construction and
         * there are buffered events, this method appends all buffered
         * entries to the file and then clears the buffer.  If no file is
         * configured, the method does nothing.
         */
        public void Flush()
        {
            if (!string.IsNullOrWhiteSpace(logFilePath) && eventBuffer.Count > 0)
            {
                File.AppendAllLines(logFilePath, eventBuffer);
                eventBuffer.Clear();
            }
        }

        /**
         * @brief Writes all buffered events to the log file without clearing.
         *
         * This convenience method writes the current contents of the buffer
         * to the file using `File.WriteAllLines`, overwriting any existing
         * file content.  Unlike @ref Flush, it does not clear the buffer,
         * so calling it repeatedly may duplicate entries in the file.
         */
        public void SaveReport()
        {
            System.IO.File.WriteAllLines(logFilePath, eventBuffer);
        }
    }
}
