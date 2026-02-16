#!/usr/bin/env python3
"""
Convert Result<T> pattern to (Result, T) tuple pattern in Wolverine handlers.
"""
import re
import sys
from pathlib import Path

def convert_handler_file(file_path):
    """Convert a single handler file from Result<T> to (Result, T?) tuple pattern."""
    content = file_path.read_text()
    original = content

    # Pattern 1: Result<EventType> in method signature
    # public static Result<ContractActivated> Handle(...)
    # -> public static (Result, ContractActivated?) Handle(...)
    content = re.sub(
        r'public static Result<(\w+)> Handle\(',
        r'public static (Result, \1?) Handle(',
        content
    )

    # Pattern 2: Failure returns
    # return Result<EventType>.Failure(error);
    # -> return (Result.Failure(error), null);
    content = re.sub(
        r'return Result<\w+>\.Failure\(([^)]+)\);',
        r'return (Result.Failure(\1), null);',
        content
    )

    # Pattern 3: Success returns with event
    # return Result<EventType>.Success(new Event(...));
    # -> return (Result.Success(), new Event(...));
    content = re.sub(
        r'return Result<\w+>\.Success\((new \w+\([^)]*\))\);',
        r'return (Result.Success(), \1);',
        content
    )

    # Pattern 4: Success returns with cast (for object type)
    # return Result<object>.Success((object)new Event(...));
    # -> return (Result.Success(), (object)new Event(...));
    content = re.sub(
        r'return Result<object>\.Success\((\(object\)new \w+\([^)]*\))\);',
        r'return (Result.Success(), \1);',
        content
    )

    if content != original:
        file_path.write_text(content)
        return True
    return False

def main():
    if len(sys.argv) < 2:
        print("Usage: python convert_to_tuple_pattern.py <file_or_directory>")
        sys.exit(1)

    path = Path(sys.argv[1])

    if path.is_file():
        files = [path]
    elif path.is_dir():
        files = list(path.rglob("*Handler.cs"))
    else:
        print(f"Error: {path} is not a file or directory")
        sys.exit(1)

    converted = 0
    for file_path in files:
        if convert_handler_file(file_path):
            print(f"✓ Converted: {file_path}")
            converted += 1
        else:
            print(f"- Skipped: {file_path}")

    print(f"\n✅ Converted {converted} files")

if __name__ == "__main__":
    main()
