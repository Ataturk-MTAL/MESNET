#!/usr/bin/env python3
"""
Complete migration from Result<T> to (Result, T?) tuple pattern.
Handles both handlers and endpoints.
"""
import re
from pathlib import Path
import sys

def migrate_handler(content):
    """Migrate a handler file to tuple pattern."""
    original = content

    # 1. Method signature: Result<EventType> -> (Result, EventType?)
    content = re.sub(
        r'public static (async Task<)?Result<(\w+)>>',
        r'public static \1(Result, \2?)',
        content
    )

    # 2. Failure returns with ) missing
    # return Result<T>.Failure(Error(...)) -> (Result.Failure(Error(...)), null)
    content = re.sub(
        r'return Result<\w+>\.Failure\(([^;]+)\);',
        r'return (Result.Failure(\1), null);',
        content
    )

    # 3. Success returns with event
    # return Result<T>.Success(new Event(...)) -> (Result.Success(), new Event(...))
    content = re.sub(
        r'return Result<\w+>\.Success\((new \w+\([^)]+\))\);',
        r'return (Result.Success(), \1);',
        content
    )

    # 4. Success returns with cast (for object)
    # return Result<object>.Success((object)new Event(...)) -> (Result.Success(), (object)new Event(...))
    content = re.sub(
        r'return Result<object>\.Success\(\((object)new \w+\([^)]+\)\)\);',
        r'return (Result.Success(), \1);',
        content
    )

    # 5. Multi-line Success with complex event
    # Handle cases where event constructor spans multiple lines
    def replace_multiline_success(match):
        event_constructor = match.group(1)
        return f'return (Result.Success(), {event_constructor});'

    content = re.sub(
        r'return Result<\w+>\.Success\((new \w+\([^;]+)\);',
        replace_multiline_success,
        content,
        flags=re.DOTALL
    )

    return content if content != original else None

def migrate_endpoint(content):
    """Migrate an endpoint file to tuple destructuring."""
    original = content

    # 1. InvokeAsync<Result<EventType>> with event data access
    #    var result = await bus.InvokeAsync<Result<EventType>>(cmd);
    # -> var (result, @event) = await bus.InvokeAsync<(Result, EventType)>(cmd);

    def replace_invoke_with_event(match):
        event_type = match.group(1)
        command = match.group(2)
        # Check if result.Value is used later
        return f'var (result, @event) = await bus.InvokeAsync<(Result, {event_type})>({command});'

    content = re.sub(
        r'var result = await bus\.InvokeAsync<Result<(\w+)>>\(([^)]+)\);',
        replace_invoke_with_event,
        content
    )

    # 2. Replace result.Value.Property with @event.Property
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
    content = file_path.read_text()

    if "Handler.cs" in file_path.name:
        migrated = migrate_handler(content)
    elif "Endpoints.cs" in file_path.name:
        migrated = migrate_endpoint(content)
    else:
        return False

    if migrated:
        file_path.write_text(migrated)
        return True
    return False

def main():
    if len(sys.argv) < 2:
        print("Usage: python migrate_to_tuple_complete.py <directory>")
        sys.exit(1)

    root = Path(sys.argv[1])

    handler_files = list(root.rglob("*Handler.cs"))
    endpoint_files = list(root.rglob("*Endpoints.cs"))

    all_files = handler_files + endpoint_files

    converted = 0
    for file_path in all_files:
        try:
            if process_file(file_path):
                print(f"✓ {file_path}")
                converted += 1
            else:
                print(f"- {file_path}")
        except Exception as e:
            print(f"✗ {file_path}: {e}")

    print(f"\n✅ Successfully converted {converted}/{len(all_files)} files")

if __name__ == "__main__":
    main()
