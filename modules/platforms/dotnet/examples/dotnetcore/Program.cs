/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Apache.Ignite.Examples
{
    using System;

    /// <summary>
    /// Examples selector - program entry point.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Runs the program.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            Write("Welcome to Apache Ignite.NET Examples!");
            Write("Choose an example to run:");
            Console.WriteLine();
            Write("1. Cache put-get");
            Write("2. SQL");
            Write("3. LINQ");
            Console.WriteLine();

            switch (ReadNumber())
            {
                case 1:
                    Write("Starting cache put-get example ...");
                    PutGetExample.Run();
                    break;
            }

            Write("Example finished, press any key to exit ...");
            Console.ReadKey();
        }

        /// <summary>
        /// Reads the number from console.
        /// </summary>
        private static int ReadNumber()
        {
            Console.WriteLine("Enter a number: ");

            while (true)
            {
                var input = Console.ReadLine();

                if (!int.TryParse(input, out var id))
                {
                    Console.WriteLine("Not a number, try again: ");
                }
                else if (id < 1 || 3 < id)
                {
                    Console.WriteLine("Out of range, try again: ");
                }
                else
                {
                    return id;
                }
            }
        }

        /// <summary>
        /// Writes the string to console.
        /// </summary>
        private static void Write(string s = null)
        {
            Console.WriteLine(s == null ? null : $">>> {s}");
        }
    }
}