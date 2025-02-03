using System;

namespace Lagrange.OneBot.DatabaseShift.Exceptions;

public class DirectoryExistedException(string message) : Exception(message) {}