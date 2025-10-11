import { createTheme, MantineColorsTuple } from '@mantine/core';

const tealAccent: MantineColorsTuple = [
  '#e6fffb',
  '#c0fef3',
  '#99f6e4',
  '#73ebd1',
  '#4edfc0',
  '#33c5a8',
  '#26a38b',
  '#1d8270',
  '#156155',
  '#0c4139'
];

const copperAccent: MantineColorsTuple = [
  '#fff0eb',
  '#ffd7ce',
  '#ffb9a6',
  '#ff9a7d',
  '#f07e5f',
  '#d66549',
  '#b8523c',
  '#964030',
  '#753025',
  '#532017'
];

const charcoal: MantineColorsTuple = [
  '#f4f4f4',
  '#d9d9d9',
  '#bebebe',
  '#a2a2a2',
  '#878787',
  '#6d6d6d',
  '#545454',
  '#3b3b3b',
  '#242424',
  '#111111'
];

export const theme = createTheme({
  fontFamily: "'Geist', sans-serif",
  headings: {
    fontFamily: "'Geist', sans-serif",
    fontWeight: 600
  },
  defaultRadius: 'md',
  primaryColor: 'tealAccent',
  primaryShade: 4,
  colors: {
    tealAccent,
    copperAccent,
    charcoal
  },
  components: {
    AppShell: {
      defaultProps: {
        padding: 'lg'
      }
    },
    Card: {
      defaultProps: {
        shadow: 'sm',
        padding: 'lg',
        radius: 'md',
        withBorder: true
      },
      styles: {
        root: {
          backgroundColor: '#252525',
          borderColor: '#333333'
        }
      }
    },
    Button: {
      defaultProps: {
        radius: 'md'
      }
    }
  }
});
