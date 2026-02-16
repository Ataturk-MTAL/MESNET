#!/usr/bin/env python3
"""
Convert endpoints to use tuple destructuring pattern.
"""
import re
import sys
from pathlib import Path

def convert_endpoint_file(file_path):
    """Convert endpoint file to use tuple destructuring."""
    content = file_path.read_text()
    original = content

    # Pattern 1: InvokeAsync<Result<EventType>>
    # var result = await bus.InvokeAsync<Result<ContractCreated>>(command);
    # -> var (result, @event) = await bus.InvokeAsync<(Result, ContractCreated)>(command);

    # First, find all InvokeAsync calls
    invoke_pattern = r'var result = await bus\.InvokeAsync<Result<(\w+)>>\(([^)]+)\);'
    matches = list(re.finditer(invoke_pattern, content))

    for match in reversed(matches):  # Process in reverse to maintain positions
        event_type = match.group(1)
        command = match.group(2)
        replacement = f'var (result, @event) = await bus.InvokeAsync<(Result, {event_type})>({command});'
        content = content[:match.start()] + replacement + content[match.end():]

    # Pattern 2: Access event data
    # result.Value.ContractId -> @event.ContractId
    content = re.sub(r'result\.Value\.(\w+)', r'@event.\1', content)

    # Pattern 3: Handle cases where Result<object> is used (aggregate handlers with multiple event types)
    content = re.sub(
        r'var result = await bus\.InvokeAsync<Result<object>>\(([^)]+)\);',
        r'var (result, _) = await bus.InvokeAsync<(Result, object?)>(\1);',
        content
    )

    if content != original:
        file_path.write_text(content)
        return True
    return False

def main():
    if len(sys.argv) < 2:
        print("Usage: python convert_endpoints_to_tuple.py <file_or_directory>")
        sys.exit(1)

    path = Path(sys.argv[1])

    if path.is_file():
        files = [path]
    elif path.is_dir():
        files = list(path.rglob("*Endpoints.cs"))
    else:
        print(f"Error: {path} is not a file or directory")
        sys.exit(1)

    converted = 0
    for file_path in files:
        if convert_endpoint_file(file_path):
            print(f"✓ Converted: {file_path}")
            converted += 1
        else:
            print(f"- Skipped: {file_path}")

    print(f"\n✅ Converted {converted} files")

if __name__ == "__main__":
    main()
