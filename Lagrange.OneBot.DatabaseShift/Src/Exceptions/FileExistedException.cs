using System;

namespace Lagrange.OneBot.DatabaseShift.Exceptions;

public class FileExistedException(string message) : Exception(message) { }