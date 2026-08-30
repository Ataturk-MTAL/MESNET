import { describe, it, expect } from 'vitest'
import { childNodeTypeFor, isSchoolNode } from './institutionTree'

describe('childNodeTypeFor', () => {
  it('il müdürlüğünün çocuğu ilçe müdürlükleridir', () => {
    expect(childNodeTypeFor('Province')).toBe('District')
  })

  it('ilçe müdürlüğünün çocuğu okullardır', () => {
    expect(childNodeTypeFor('District')).toBe('School')
  })

  it('okul düğümünün çocuğu yoktur', () => {
    expect(childNodeTypeFor('School')).toBeNull()
  })

  it('tanınmayan tip için null döner', () => {
    expect(childNodeTypeFor('Unknown')).toBeNull()
  })
})

describe('isSchoolNode', () => {
  it('School için true döner', () => {
    expect(isSchoolNode('School')).toBe(true)
  })

  it('District/Province için false döner', () => {
    expect(isSchoolNode('District')).toBe(false)
    expect(isSchoolNode('Province')).toBe(false)
  })
})
