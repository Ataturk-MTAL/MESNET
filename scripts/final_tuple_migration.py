#!/usr/bin/env python3
"""
Final tuple pattern migration - tested and verified approach.
"""
import re
from pathlib import Path
import sys

def migrate_handler(content, filename):
    """Migrate handler to tuple pattern."""
    original = content

    # 1. Method signature: Result<EventType> Handle -> (Result, EventType) Handle
    content = re.sub(
        r'public static Result<(\w+)> Handle\(',
        r'public static (Result, \1) Handle(',
        content
    )

    # 2. Method signature with async: Task<Result<EventType>> -> Task<(Result, EventType)>
    content = re.sub(
        r'public static async Task<Result<(\w+)>> Handle\(',
        r'public static async Task<(Result, \1)> Handle(',
        content
    )

    # 3. Aggregate handlers with [AggregateHandler]
    content = re.sub(
        r'\[AggregateHandler\]\s+public static Result<(\w+)> Handle\(',
        r'[AggregateHandler]\n    public static (Result, \1?) Handle(',
        content
    )

    # 4. Aggregate handlers async
    content = re.sub(
        r'\[AggregateHandler\]\s+public static async Task<Result<(\w+)>> Handle\(',
        r'[AggregateHandler]\n    public static async Task<(Result, \1?)> Handle(',
        content
    )

    # 5. Failure returns: Result<T>.Failure(...) -> (Result.Failure(...), null)
    # Handle single line
    content = re.sub(
        r'return Result<\w+>\.Failure\(([^;]+?)\);',
        r'return (Result.Failure(\1), null);',
        content,
        flags=re.DOTALL
    )

    # 6. Success returns: Result<T>.Success(new Event(...)) -> (Result.Success(), new Event(...))
    # Single line version
    content = re.sub(
        r'return Result<\w+>\.Success\((new \w+[^;]+?)\);',
        r'return (Result.Success(), \1);',
        content,
        flags=re.DOTALL
    )

    # 7. Success with cast: Result<object>.Success((object)new ...) -> (Result.Success(), (object)new ...)
    content = re.sub(
        r'return Result<object>\.Success\(\((object)([^;]+?)\)\);',
        r'return (Result.Success(), (object)\2);',
        content,
        flags=re.DOTALL
    )

    return content if content != original else None

def migrate_endpoint(content, filename):
    """Migrate endpoint to tuple destructuring."""
    original = content

    # 1. InvokeAsync<Result<EventType>> with simple assignment
    # var result = await bus.InvokeAsync<Result<EventType>>(...)
    # -> var (result, @event) = await bus.InvokeAsync<(Result, EventType)>(...)
    content = re.sub(
        r'var result = await bus\.InvokeAsync<Result<(\w+)>>\(([^)]+)\);',
        r'var (result, @event) = await bus.InvokeAsync<(Result, \1)>(\2);',
        content
    )

    # 2. Replace result.Value with @event
    content = re.sub(r'result\.Value\.', r'@event.', content)

    # 3. InvokeAsync<Result<object>> for aggregate handlers
    content = re.sub(
        r'var result = await bus\.InvokeAsync<Result<object>>\(([^)]+)\);',
        r'var (result, _) = await bus.InvokeAsync<(Result, object?)>(\1);',
        content
    )

    return content if content != original else None

def process_file(file_path):
    """Process a single file."""
    try:
        content = file_path.read_text(encoding='utf-8')

        if "Handler.cs" in file_path.name:
            migrated = migrate_handler(content, file_path.name)
        elif "Endpoints.cs" in file_path.name:
            migrated = migrate_endpoint(content, file_path.name)
        else:
            return False

        if migrated:
            file_path.write_text(migrated, encoding='utf-8')
            return True
        return False
    except Exception as e:
        print(f"✗ Error processing {file_path}: {e}")
        return False

def main():
    if len(sys.argv) < 2:
        print("Usage: python final_tuple_migration.py <module_directory>")
        sys.exit(1)

    root = Path(sys.argv[1])

    # Process only specific modules
    modules = ["Payment", "Attendance", "Internship"]

    converted = 0
    skipped = 0

    for module in modules:
        module_path = root / module
        if not module_path.exists():
            continue

        handler_files = list(module_path.rglob("*Handler.cs"))
        endpoint_files = list(module_path.rglob("*Endpoints.cs"))

        all_files = handler_files + endpoint_files

        for file_path in all_files:
            if process_file(file_path):
                print(f"✓ {file_path.relative_to(root)}")
                converted += 1
            else:
                print(f"- {file_path.relative_to(root)}")
                skipped += 1

    print(f"\n✅ Converted: {converted} files")
    print(f"⊘  Skipped: {skipped} files")

if __name__ == "__main__":
    main()
